import torch
import torch.nn as nn
import torch.optim as optim
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import torch.nn.functional as F

class FinancialAttentionLSTM(nn.Module):
    def __init__(self, input_size=6, hidden_size=20):
        super().__init__()
        self.lstm = nn.LSTM(input_size, hidden_size, batch_first=True)
        self.linear = nn.Linear(hidden_size, 1)

    def forward(self, x):
        lstm_out, (h_n, c_n) = self.lstm(x)
        final_hidden = h_n[0] 
        query = final_hidden.unsqueeze(2)
        scores = torch.bmm(lstm_out, query)
        attention_weights = F.softmax(scores, dim=1)
        weights_transposed = attention_weights.transpose(1, 2)
        context_vector = torch.bmm(weights_transposed, lstm_out)
        context_vector = context_vector.squeeze(1)
        prediction = self.linear(context_vector)
        return prediction

if __name__ == "__main__":

    TEST_SIZE: int = 50  # Amount of rows to be used as test data /From closest date
    SEQ_LENGTH: int = 10 # Sequence size

    df: pd.DataFrame = pd.read_csv("all_bist100.csv")

    delta = df['Close'].diff() # Calculate close changes between days t1-t0

    # Separate the positive gains and negative losses
    gain = delta.where(delta > 0, 0)
    loss = -delta.where(delta < 0, 0)

    # Calculate exponential moving average of gains and losses
    avg_gain = gain.ewm(span=10, adjust=False).mean()
    avg_loss = loss.ewm(span=10, adjust=False).mean()

    # 4. Calculate the Relative Strength (RS) and the RSI
    rs = avg_gain / avg_loss
    df['RSI'] = 100 - (100 / (1 + rs))

    # 5. Drop the first 14 rows because they will have NaN values
    df = df.dropna().reset_index(drop=True)

    df['RSI_Normalized'] = (df['RSI'] / 50.0) - 1.0

    feature_cols = ['Open', 'High', 'Low', 'Close', 'Volume', 'RSI_Normalized']
    inputs_np = df[feature_cols].values.astype('float32')

    x, y, bases = [], [], []

    for i in range(len(inputs_np) - SEQ_LENGTH):
        window = inputs_np[i : i + SEQ_LENGTH].copy() # A Single Input Sequence
        target_open = inputs_np[i + SEQ_LENGTH, 0]    # Target Day Open Price (Label)

        base_price = window[0, 0]  # First Day Open Price
        base_vol = window[0, 4] if window[0, 4] > 0 else 1.0 # First Day Volume

        window[:, 0:4] = (window[:, 0:4] / base_price) - 1.0  # Normalize Open High Low Close / Based on first days' Open
        window[:, 4] = (window[:, 4] / base_vol) - 1.0        # Normalize Volume / Based on first days' Volume 
        normalized_target = (target_open / base_price) - 1.0  # Normalize the target Open
        x.append(window)
        y.append(normalized_target)
        bases.append(base_price)

    x = torch.tensor(np.array(x), dtype=torch.float32)
    y = torch.tensor(np.array(y), dtype=torch.float32).unsqueeze(1)
    bases = np.array(bases).reshape(-1, 1)

    train_size = len(x) - TEST_SIZE
    X_train, X_test = x[:train_size], x[train_size:]
    y_train, y_test = y[:train_size], y[train_size:]
    bases_train, bases_test = bases[:train_size], bases[train_size:]

    model = FinancialAttentionLSTM()
    criterion = nn.MSELoss()
    optimizer = optim.Adam(model.parameters(), lr=0.001)

    epochs = 10000
    print(f"Training on {len(X_train)} days of data...")
    for epoch in range(epochs):
        model.train()
        optimizer.zero_grad()
        predictions = model(X_train)
        loss = criterion(predictions, y_train)
        scheduler = optim.lr_scheduler.ReduceLROnPlateau(optimizer, 'min', patience=10)

        loss.backward()
        optimizer.step()
        scheduler.step(loss)
        
        if (epoch + 1) % 20 == 0:
            print(f"Epoch {epoch+1:3} | Loss: {loss.detach().item():.6f}")

    model.eval()
    with torch.no_grad():
        test_preds_normalized = torch.Tensor.cpu(model(X_test)).numpy()
        
        test_preds_real = bases_test * (test_preds_normalized + 1.0)
        actual_real = bases_test * (torch.Tensor.cpu(y_test).numpy() + 1.0)


    last_10_days_raw = inputs_np[-SEQ_LENGTH:].copy()
    future_base_price = last_10_days_raw[0, 0]
    future_base_vol = last_10_days_raw[0, 4] if last_10_days_raw[0, 4] > 0 else 1.0

    last_10_days_raw[:, 0:4] = (last_10_days_raw[:, 0:4] / future_base_price) - 1.0
    last_10_days_raw[:, 4] = (last_10_days_raw[:, 4] / future_base_vol) - 1.0

    last_10_days_tensor = torch.tensor(last_10_days_raw, dtype=torch.float32).unsqueeze(0)

    with torch.no_grad():
        future_pred_normalized = torch.Tensor.cpu(model(last_10_days_tensor)).numpy()
        future_pred_real = future_base_price * (future_pred_normalized[0][0] + 1.0)

    torch.save(model.state_dict(), 'LSTM_Attention.pth')

    plt.figure(figsize=(12,6))
    plt.plot(actual_real, color="black", label="Actual Open Price", linewidth=2)
    plt.plot(test_preds_real, color='green', label="Predicted Open Price", linestyle='--')
    plt.title("BIST100 Open Price Prediction (Scale-Invariant Model)")
    plt.legend()
    plt.show()