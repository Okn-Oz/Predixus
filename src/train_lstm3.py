import torch
import torch.nn as nn
import torch.optim as optim
import pandas as pd
import numpy as np
import matplotlib
matplotlib.use('Agg')  # Headless safety (WSL/server)
import matplotlib.pyplot as plt
import torch.nn.functional as F
from src.config import DEVICE

SEED = 42
torch.manual_seed(SEED)
np.random.seed(SEED)


class FinancialAttentionLSTM(nn.Module):
    def __init__(self, input_size=6, hidden_size=20):
        super().__init__()
        self.lstm = nn.LSTM(input_size, hidden_size, batch_first=True)
        self.linear = nn.Linear(hidden_size, 1)

    def forward(self, x):
        lstm_out, (h_n, _) = self.lstm(x)
        final_hidden = h_n[0]
        query = final_hidden.unsqueeze(2)
        scores = torch.bmm(lstm_out, query)
        attention_weights = F.softmax(scores, dim=1)
        weights_transposed = attention_weights.transpose(1, 2)
        context_vector = torch.bmm(weights_transposed, lstm_out)
        context_vector = context_vector.squeeze(1)
        prediction = self.linear(context_vector)
        return prediction


def compute_rsi(close: pd.Series, span: int = 14) -> pd.Series:
    """Wilder-style RSI using EWM. Shared by training and the API."""
    delta = close.diff()
    gain = delta.where(delta > 0, 0.0)
    down = -delta.where(delta < 0, 0.0)
    avg_gain = gain.ewm(span=span, adjust=False).mean()
    avg_down = down.ewm(span=span, adjust=False).mean()
    rs = avg_gain / avg_down.replace(0, np.nan)
    return 100 - (100 / (1 + rs))


if __name__ == "__main__":

    TEST_SIZE: int = 50    # Held-out final evaluation
    VAL_SIZE: int = 50     # For early stopping
    SEQ_LENGTH: int = 10
    RSI_SPAN: int = 14     # Match the API
    EPOCHS: int = 1000
    PATIENCE: int = 30     # Early-stopping patience (epochs without val improvement)
    LR: float = 0.001

    df: pd.DataFrame = pd.read_csv("data/all_bist100.csv")
    df['RSI'] = compute_rsi(df['Close'], span=RSI_SPAN)
    df = df.dropna().reset_index(drop=True)
    df['RSI_Normalized'] = (df['RSI'] / 50.0) - 1.0

    feature_cols = ['Open', 'High', 'Low', 'Close', 'Volume', 'RSI_Normalized']
    inputs_np = df[feature_cols].values.astype('float32')

    x, y, bases, last_closes = [], [], [], []

    for i in range(len(inputs_np) - SEQ_LENGTH):
        window = inputs_np[i : i + SEQ_LENGTH].copy()  # A single input sequence
        target_open = inputs_np[i + SEQ_LENGTH, 0]      # Next-day Open (label)

        base_price = window[0, 0]                       # First-day Open
        base_vol = window[0, 4] if window[0, 4] > 0 else 1.0
        last_close_in_window = window[-1, 3]            # For naive baseline (raw, pre-norm)

        window[:, 0:4] = (window[:, 0:4] / base_price) - 1.0  # Normalize OHLC
        window[:, 4] = (window[:, 4] / base_vol) - 1.0         # Normalize Volume
        normalized_target = (target_open / base_price) - 1.0   # Normalize target

        x.append(window)
        y.append(normalized_target)
        bases.append(base_price)
        last_closes.append(last_close_in_window)

    x = torch.tensor(np.array(x), dtype=torch.float32)
    y = torch.tensor(np.array(y), dtype=torch.float32).unsqueeze(1)
    bases = np.array(bases).reshape(-1, 1)
    last_closes = np.array(last_closes).reshape(-1, 1)

    train_size = len(x) - TEST_SIZE - VAL_SIZE
    val_end = train_size + VAL_SIZE
    X_train, X_val, X_test = x[:train_size], x[train_size:val_end], x[val_end:]
    y_train, y_val, y_test = y[:train_size], y[train_size:val_end], y[val_end:]
    bases_test = bases[val_end:]
    last_closes_test = last_closes[val_end:]

    model = FinancialAttentionLSTM().to(DEVICE)
    criterion = nn.MSELoss()
    optimizer = optim.Adam(model.parameters(), lr=LR)
    scheduler = optim.lr_scheduler.ReduceLROnPlateau(optimizer, 'min', patience=10)

    X_train = X_train.to(DEVICE)
    y_train = y_train.to(DEVICE)
    X_val = X_val.to(DEVICE)
    y_val = y_val.to(DEVICE)

    print(f"Training on {len(X_train)} days | Val: {len(X_val)} | Test: {len(X_test)}")

    best_val_loss = float('inf')
    best_state = None
    epochs_no_improve = 0

    for epoch in range(EPOCHS):
        model.train()
        optimizer.zero_grad()
        predictions = model(X_train)
        train_loss = criterion(predictions, y_train)
        train_loss.backward()
        optimizer.step()

        model.eval()
        with torch.no_grad():
            val_loss = criterion(model(X_val), y_val)

        scheduler.step(val_loss)

        if val_loss.item() < best_val_loss:
            best_val_loss = val_loss.item()
            best_state = {k: v.detach().cpu().clone() for k, v in model.state_dict().items()}
            epochs_no_improve = 0
        else:
            epochs_no_improve += 1

        if (epoch + 1) % 20 == 0:
            print(f"Epoch {epoch+1:4d} | train: {train_loss.item():.6f} | val: {val_loss.item():.6f} | best_val: {best_val_loss:.6f}")

        if epochs_no_improve >= PATIENCE:
            print(f"Early stopping at epoch {epoch+1}. Best val loss: {best_val_loss:.6f}")
            break

    if best_state is not None:
        model.load_state_dict(best_state)

    model.eval()
    with torch.no_grad():
        test_preds_normalized = model(X_test.to(DEVICE)).cpu().numpy()
        test_preds_real = bases_test * (test_preds_normalized + 1.0)
        actual_real = bases_test * (y_test.cpu().numpy() + 1.0)

    # Naive baseline: next-day Open ≈ today's Close
    naive_mse = float(np.mean((actual_real - last_closes_test) ** 2))
    model_mse = float(np.mean((actual_real - test_preds_real) ** 2))
    naive_mae = float(np.mean(np.abs(actual_real - last_closes_test)))
    model_mae = float(np.mean(np.abs(actual_real - test_preds_real)))

    print()
    print("=== Test Set Metrics ===")
    print(f"Naive (last Close):      MSE={naive_mse:,.2f}  MAE={naive_mae:,.2f}")
    print(f"Model (LSTM+Attention):  MSE={model_mse:,.2f}  MAE={model_mae:,.2f}")
    if model_mse < naive_mse:
        print(f"Model beats baseline by {(1 - model_mse/naive_mse)*100:.1f}% (MSE).")
    else:
        print(f"WARNING: Model is {(model_mse/naive_mse - 1)*100:.1f}% WORSE than naive baseline.")

    # One-day-ahead future prediction (uses the last SEQ_LENGTH days of the dataset)
    last_window_raw = inputs_np[-SEQ_LENGTH:].copy()
    future_base_price = last_window_raw[0, 0]
    future_base_vol = last_window_raw[0, 4] if last_window_raw[0, 4] > 0 else 1.0
    last_window_raw[:, 0:4] = (last_window_raw[:, 0:4] / future_base_price) - 1.0
    last_window_raw[:, 4] = (last_window_raw[:, 4] / future_base_vol) - 1.0
    last_window_tensor = torch.tensor(last_window_raw, dtype=torch.float32).unsqueeze(0).to(DEVICE)

    with torch.no_grad():
        future_pred_normalized = model(last_window_tensor).cpu().numpy()
        future_pred_real = future_base_price * (future_pred_normalized[0][0] + 1.0)

    print(f"\nNext-day Open prediction: {future_pred_real:.2f}")

    torch.save(model.state_dict(), 'models/LSTM_Attention.pth')
    print("Model saved to models/LSTM_Attention.pth")

    plt.figure(figsize=(12, 6))
    plt.plot(actual_real, color="black", label="Actual Open Price", linewidth=2)
    plt.plot(test_preds_real, color='green', label="Predicted Open Price", linestyle='--')
    plt.plot(last_closes_test, color='gray', label="Naive (last Close)", linestyle=':', alpha=0.7)
    plt.title("BIST100 Open Price Prediction (Scale-Invariant Model)")
    plt.legend()
    plt.savefig('models/test_predictions.png', dpi=100, bbox_inches='tight')
    print("Plot saved to models/test_predictions.png")
