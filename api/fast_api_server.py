from fastapi import FastAPI, UploadFile, File
from pydantic import BaseModel
import torch
import io
import pandas as pd
from src.train_lstm3 import FinancialAttentionLSTM 
import uvicorn

app = FastAPI()
model = FinancialAttentionLSTM()

weights = torch.load('models/LSTM_Attention.pth', map_location=torch.device('cpu'))

model.load_state_dict(weights)
model.eval()

class InferenceResponse(BaseModel):
    prediction: str

@app.post("/predict", response_model=InferenceResponse)
async def predict(file: UploadFile = File(...)):
    contents = await file.read()
    csv_string = contents.decode('utf-8')
    df = pd.read_csv(io.StringIO(csv_string))

    delta = df['Close'].diff() 
    gain = delta.where(delta > 0, 0)
    loss = -delta.where(delta < 0, 0)

    avg_gain = gain.ewm(span=14, adjust=False).mean()
    avg_loss = loss.ewm(span=14, adjust=False).mean()

    rs = avg_gain / avg_loss
    df['RSI'] = 100 - (100 / (1 + rs))

    df = df.dropna().reset_index(drop=True)
    df['RSI_Normalized'] = (df['RSI'] / 50.0) - 1.0

    feature_cols = ['Open', 'High', 'Low', 'Close', 'Volume', 'RSI_Normalized']
    inputs_np = df[feature_cols].values.astype('float32')
    
    if len(inputs_np) < 10:
        return {"prediction": "Error: CSV must contain at least 24 days of data (14 for RSI + 10 for sequence)."}

    last_10_days = inputs_np[-10:].copy()

    base_price = last_10_days[0, 0]  
    base_vol = last_10_days[0, 4] if last_10_days[0, 4] > 0 else 1.0

    last_10_days[:, 0:4] = (last_10_days[:, 0:4] / base_price) - 1.0  
    last_10_days[:, 4] = (last_10_days[:, 4] / base_vol) - 1.0        

    tensor_data = torch.tensor(last_10_days, dtype=torch.float32).unsqueeze(0).to('cpu')

    with torch.no_grad():
        output_normalized = model(tensor_data)
        
        pred_val = output_normalized.item() 
        
        predicted_real_price = base_price * (pred_val + 1.0)

    prediction_result = f"{predicted_real_price:.2f}"

    return {"prediction": prediction_result}

if __name__ == "__main__":
    uvicorn.run("fast_api_server:app", host="0.0.0.0", port=8000, reload=True)