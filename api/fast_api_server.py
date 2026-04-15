from fastapi import FastAPI, UploadFile, File, HTTPException
from pydantic import BaseModel
import torch
import io
import pandas as pd
from src.train_lstm3 import FinancialAttentionLSTM, compute_rsi
import uvicorn
from src.config import DEVICE

SEQ_LENGTH = 10
RSI_SPAN = 14  # MUST match training (src/train_lstm3.py)

app = FastAPI()
model = FinancialAttentionLSTM()

weights = torch.load(
    'models/LSTM_Attention.pth',
    map_location=torch.device(DEVICE),
    weights_only=True,
)
model.load_state_dict(weights)
model.to(DEVICE)
model.eval()


class InferenceResponse(BaseModel):
    prediction: str


@app.post("/predict", response_model=InferenceResponse)
async def predict(file: UploadFile = File(...)):
    contents = await file.read()
    try:
        csv_string = contents.decode('utf-8')
        df = pd.read_csv(io.StringIO(csv_string))
    except Exception as e:
        raise HTTPException(status_code=400, detail=f"Could not parse CSV: {e}")

    required = {'Open', 'High', 'Low', 'Close', 'Volume'}
    missing = required - set(df.columns)
    if missing:
        raise HTTPException(
            status_code=400,
            detail=f"Missing columns: {sorted(missing)}",
        )

    df['RSI'] = compute_rsi(df['Close'], span=RSI_SPAN)
    df = df.dropna().reset_index(drop=True)
    df['RSI_Normalized'] = (df['RSI'] / 50.0) - 1.0

    feature_cols = ['Open', 'High', 'Low', 'Close', 'Volume', 'RSI_Normalized']
    inputs_np = df[feature_cols].values.astype('float32')

    if len(inputs_np) < SEQ_LENGTH:
        raise HTTPException(
            status_code=400,
            detail=(
                f"CSV needs at least {RSI_SPAN + SEQ_LENGTH} rows "
                f"({RSI_SPAN} for RSI warm-up + {SEQ_LENGTH} for the sequence)."
            ),
        )

    last_window = inputs_np[-SEQ_LENGTH:].copy()
    base_price = last_window[0, 0]
    base_vol = last_window[0, 4] if last_window[0, 4] > 0 else 1.0
    last_window[:, 0:4] = (last_window[:, 0:4] / base_price) - 1.0
    last_window[:, 4] = (last_window[:, 4] / base_vol) - 1.0

    tensor_data = torch.tensor(last_window, dtype=torch.float32).unsqueeze(0).to(DEVICE)
    with torch.no_grad():
        pred_val = model(tensor_data).item()
        predicted_real_price = base_price * (pred_val + 1.0)

    return {"prediction": f"{predicted_real_price:.2f}"}


if __name__ == "__main__":
    uvicorn.run("api.fast_api_server:app", host="0.0.0.0", port=8000, reload=True)
