import torch
import torch.nn as nn
import torch.optim as optim
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import torch.nn.functional as F
from torch.utils.data import TensorDataset, DataLoader
from src.config import DEVICE
from models.templates.lstm import FinancialLSTM
import src.py.utils as utils

pd.set_option('display.max_rows', None)

def CalculateRSI(df: pd.DataFrame, span: int = 14) -> pd.DataFrame:
    delta = df['Close'].diff()
    gain = delta.where(delta > 0, 0)
    loss = -delta.where(delta < 0, 0)

    avg_gain = gain.ewm(span=span, adjust=False).mean()
    avg_loss = loss.ewm(span=span, adjust=False).mean()

    rs = avg_gain / avg_loss
    df['RSI'] = 100 - (100 / (1 + rs))

    df['RSI_Normalized'] = (df['RSI'] / 50.0) - 1.0
    return df

import pandas as pd

def CalculateNATR(df: pd.DataFrame, period: int = 14) -> pd.DataFrame:
    tr1 = df['High'] - df['Low']
    tr2 = (df['High'] - df['Close'].shift(1)).abs()
    tr3 = (df['Low'] - df['Close'].shift(1)).abs()
    
    tr = pd.concat([tr1, tr2, tr3], axis=1).max(axis=1)
    df['ATR'] = tr.ewm(alpha=1/period, adjust=False).mean()

    df['NATR'] = (df['ATR'] / df['Close']) * 100
    
    rolling_mean = df['NATR'].rolling(window=100).mean()
    rolling_std = df['NATR'].rolling(window=100).std()
    df['NATR_Normalized'] = (df['NATR'] - rolling_mean) / rolling_std
    
    return df

def CalculateMACD(df: pd.DataFrame, fast: int = 12, slow: int = 26, signal: int = 9, window: int = 100) -> pd.DataFrame:
    fast_ema = df['Close'].ewm(span=fast, adjust=False).mean()
    slow_ema = df['Close'].ewm(span=slow, adjust=False).mean()

    df['MACD'] = fast_ema - slow_ema
    df['Signal_Line'] = df['MACD'].ewm(span=signal, adjust=False).mean()
    df['MACD_Histogram'] = df['MACD'] - df['Signal_Line']

    macd_pct = df['MACD'] / df['Close']
    signal_pct = df['Signal_Line'] / df['Close']
    hist_pct = df['MACD_Histogram'] / df['Close']

    df['MACD_Standardized'] = (macd_pct - macd_pct.rolling(window).mean()) / macd_pct.rolling(window).std()
    df['Signal_Standardized'] = (signal_pct - signal_pct.rolling(window).mean()) / signal_pct.rolling(window).std()
    df['Histogram_Standardized'] = (hist_pct - hist_pct.rolling(window).mean()) / hist_pct.rolling(window).std()

    return df

def CreateNormalizedSequence(df: pd.DataFrame, features: list[str], seq_length: int = 10):
    inputs_np = df[features].values.astype('float32')
    x, y, bases = [], [], []

    for i in range(len(inputs_np) - seq_length):
        window = inputs_np[i : i + seq_length].copy() 
        target_open = inputs_np[i + seq_length, 0]    

        base_price = window[0, 0]  
        base_vol = window[0, 4] if window[0, 4] > 0 else 1.0 

        window[:, 0:4] = (window[:, 0:4] / base_price) - 1.0  
        window[:, 4] = (window[:, 4] / base_vol) - 1.0        
        normalized_target = (target_open / base_price) - 1.0  
        
        x.append(window)
        y.append(normalized_target)
        bases.append(base_price)

    return x, y, bases

def Preprocess(df: pd.DataFrame, rsi_span: int, atr_span: int, macd_fast: int, macd_slow: int, macd_signal: int, macd_window: int) -> pd.DataFrame:
    df = CalculateRSI(df, rsi_span)
    df = CalculateNATR(df, atr_span)
    df = CalculateMACD(df, macd_fast, macd_slow, macd_signal, macd_window)

    df = df.dropna().reset_index(drop=True)
    return df

def TrainLSTM(data_path: str, seq_len: int, test_size: int, hidden_size: int, num_layers: int, dropout: float, 
              learning_rate: float, batch_size: int, epochs: int, rsi_span: int, atr_span: int, 
              macd_fast: int, macd_slow: int, macd_signal: int, macd_window: int, features: list[str]):
    
    df: pd.DataFrame = pd.read_csv(data_path)
    
    df = Preprocess(df, rsi_span, atr_span, macd_fast, macd_slow, macd_signal, macd_window)
    
    x, y, bases = CreateNormalizedSequence(df, features, seq_len)

    x = torch.tensor(np.array(x), dtype=torch.float32)
    y = torch.tensor(np.array(y), dtype=torch.float32).unsqueeze(1)
    bases = np.array(bases).reshape(-1, 1)

    train_size = len(x) - test_size
    X_train, X_test = x[:train_size], x[train_size:]
    y_train, y_test = y[:train_size], y[train_size:]
    bases_train, bases_test = bases[:train_size], bases[train_size:]

    train_dataset = TensorDataset(X_train, y_train)
    train_loader = DataLoader(train_dataset, batch_size, shuffle=False)

    model = FinancialLSTM(len(features), hidden_size, dropout, num_layers).to(DEVICE).float()
    criterion = nn.MSELoss()
    optimizer = optim.AdamW(model.parameters(), lr=learning_rate)
    
    scheduler = optim.lr_scheduler.ReduceLROnPlateau(optimizer, 'min', patience=10000)

    X_test_gpu = X_test.to(DEVICE)
    y_test_gpu = y_test.to(DEVICE)

    print(f"Training on {len(X_train)} days of data using batch size {batch_size}...")
    
    for epoch in range(epochs):
        model.train() 
        epoch_loss = 0.0
        
        for batch_X, batch_y in train_loader:
            batch_X = batch_X.to(DEVICE)
            batch_y = batch_y.to(DEVICE)

            optimizer.zero_grad()
            predictions = model(batch_X)
            loss = criterion(predictions, batch_y)

            loss.backward()
            optimizer.step()
            
            epoch_loss += loss.item()

        avg_train_loss = epoch_loss / len(train_loader)
        
        model.eval() 
        with torch.no_grad(): 
            val_predictions = model(X_test_gpu)
            val_loss = criterion(val_predictions, y_test_gpu)
        
        scheduler.step(val_loss.item())
        
        if (epoch + 1) % 20 == 0:
            print(f"Epoch {epoch+1:4} | Train Loss: {avg_train_loss:.6f} | Val Loss: {val_loss.item():.6f}")

    model.eval()
    with torch.no_grad():
        test_preds_normalized = model(X_test.to(DEVICE)).cpu().numpy()
        
        test_preds_real = bases_test * (test_preds_normalized + 1.0)
        actual_real = bases_test * (torch.Tensor.cpu(y_test).numpy() + 1.0)

    torch.save(model.state_dict(), 'models/LSTM_Attention.pth')

    plt.figure(figsize=(12,6))
    plt.plot(actual_real, color="black", label="Actual Open Price", linewidth=2)
    plt.plot(test_preds_real, color='green', label="Predicted Open Price", linestyle='--')
    plt.title("BIST100 Open Price Prediction (Scale-Invariant Model)")
    plt.legend()
    plt.show()