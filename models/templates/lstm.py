import torch
import torch.nn as nn
import torch.nn.functional as F

class FinancialLSTM(nn.Module):
    def __init__(self, input_size=7, hidden_size=20, dropout=0.2, num_layers = 1):
        super().__init__()
        self.lstm = nn.LSTM(input_size, hidden_size, num_layers, batch_first=True)
        self.dropout = nn.Dropout(dropout)
        self.linear1 = nn.Linear(hidden_size, 30)
        self.relu = nn.ReLU()
        self.linear2 = nn.Linear(30, 1)

    def forward(self, x):
        lstm_out, (h_n, c_n) = self.lstm(x)
        final_hidden = h_n[0]
        final_hidden = self.dropout(final_hidden)     
        prediction_linear1 = self.linear1(final_hidden)
        prediction_relu = self.relu(prediction_linear1)
        prediction_relu = self.dropout(prediction_relu)
        prediction = self.linear2(prediction_relu)
        return prediction