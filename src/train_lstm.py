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

# Model Parameters
BATCH_SIZE: int = 128  # Training Batches
SEQ_LENGTH: int = 10    # Train Sequence
TEST_SIZE: int = 200    # Test length  
EPOCHS: int = 200      # Total Epochs
DROPOUT: float = 0.1    # Dropout percentage
LR: float = 0.00087      # Learning Rate
NUM_LAYERS: int = 2     # Number of layers in LSTM
HIDDEN_SIZE: int = 96   # Hidden Size of LSTM

# Feature Parameters
RSI_SPAN: int = 10      # Relative Strength Index Lookback window
ATR_SPAN: int = 10      # Average True Range Window
MACD_FAST: int = 8     # Moving Average Convergence Divergence Fast Window
MACD_SLOW: int = 26     # Moving Average Convergence Divergence Slow Window
MACD_SIGNAL: int = 12    # Moving Average Convergence Divergence Signal Window
MACD_WINDOW: int = 40  # Moving Average Convergence Divergence Standardization Window
DATA_PATH: str = "data/all_bist100.csv" 
FEATURES: list[str] = ['Open', 'High', 'Low', 'Close', 'Volume', 'RSI_Normalized', 'NATR_Normalized', 
                       'MACD_Standardized', 'Signal_Standardized', 'Histogram_Standardized']


if __name__ == "__main__":
    utils.TrainLSTM(DATA_PATH, SEQ_LENGTH, TEST_SIZE, HIDDEN_SIZE, NUM_LAYERS, 
                    DROPOUT, LR, BATCH_SIZE, EPOCHS, RSI_SPAN, ATR_SPAN, MACD_FAST, 
                    MACD_SLOW, MACD_SIGNAL, MACD_WINDOW, FEATURES)




