import torch
import torch.nn as nn
import torch.optim as optim
import pandas as pd
import numpy as np
import optuna
from torch.utils.data import TensorDataset, DataLoader
from src.config import DEVICE
from models.templates.lstm import FinancialLSTM
import src.py.utils as utils

# ── Fixed parameters (not tuned) ─────────────────────────────────────────────
DATA_PATH   = "data/all_bist100.csv"
TEST_SIZE   = 500
EPOCHS      = 200       # keep low during tuning — speed matters
N_TRIALS    = 100       # increased — more params to search now

FEATURES: list[str] = [
    'Open', 'High', 'Low', 'Close', 'Volume',
    'RSI_Normalized', 'NATR_Normalized',
    'MACD_Standardized', 'Signal_Standardized', 'Histogram_Standardized'
]

# ─────────────────────────────────────────────────────────────────────────────
# Load raw data once — preprocessing happens inside objective
# because feature params are now being tuned
# ─────────────────────────────────────────────────────────────────────────────
print("Loading raw data...")
df_raw = pd.read_csv(DATA_PATH)
print(f"Raw dataframe shape: {df_raw.shape}")


def objective(trial: optuna.Trial) -> float:
    """
    Optuna calls this function for each trial.
    It must return a single float — the validation loss.
    Optuna tries to MINIMIZE this value.
    """

    # ── Model hyperparameters ─────────────────────────────────────────────────
    seq_len     = trial.suggest_categorical('seq_len',     [5, 10])
    hidden_size = trial.suggest_categorical('hidden_size', [96, 128])
    num_layers  = trial.suggest_int('num_layers',  1, 5)
    dropout     = trial.suggest_float('dropout',   0.1, 0.5, step=0.1)
    lr          = trial.suggest_float('lr',        1e-4, 1e-2, log=True)
    batch_size  = trial.suggest_categorical('batch_size',  [64, 128, 256, 512])

    # ── Feature engineering hyperparameters ───────────────────────────────────
    rsi_span    = trial.suggest_categorical('rsi_span',    [7, 10, 14, 21])
    atr_span    = trial.suggest_categorical('atr_span',    [7, 10, 14, 21])
    macd_fast   = trial.suggest_categorical('macd_fast',   [8, 12, 16])
    macd_slow   = trial.suggest_categorical('macd_slow',   [21, 26, 30])
    macd_signal = trial.suggest_categorical('macd_signal', [7, 9, 12])
    macd_window = trial.suggest_categorical('macd_window', [40, 50, 100, 150])

    # ── Preprocess with this trial's feature parameters ───────────────────────
    # We must copy df_raw so each trial starts from clean untouched data
    df = df_raw.copy()
    df = utils.Preprocess(df, rsi_span, atr_span, macd_fast, macd_slow, macd_signal, macd_window)

    # ── Build sequences with this trial's seq_len ─────────────────────────────
    x, y, bases = utils.CreateNormalizedSequence(df, FEATURES, seq_len)

    x = torch.tensor(np.array(x), dtype=torch.float32)
    y = torch.tensor(np.array(y), dtype=torch.float32).unsqueeze(1)

    # ── Walk-forward split — always past → future, never shuffle ─────────────
    train_size  = len(x) - TEST_SIZE
    X_train, X_test = x[:train_size], x[train_size:]
    y_train, y_test = y[:train_size], y[train_size:]

    train_dataset = TensorDataset(X_train, y_train)
    train_loader  = DataLoader(train_dataset, batch_size=batch_size, shuffle=False)

    # ── Model ─────────────────────────────────────────────────────────────────
    model     = FinancialLSTM(len(FEATURES), hidden_size, dropout, num_layers).to(DEVICE).float()
    criterion = nn.MSELoss()
    optimizer = optim.AdamW(model.parameters(), lr=lr)

    X_test_gpu = X_test.to(DEVICE)
    y_test_gpu = y_test.to(DEVICE)

    best_val_loss = float('inf')
    patience_counter = 0
    EARLY_STOP_PATIENCE = 20  # stop trial early if no improvement

    # ── Training loop ─────────────────────────────────────────────────────────
    for epoch in range(EPOCHS):
        model.train()
        for batch_X, batch_y in train_loader:
            batch_X = batch_X.to(DEVICE)
            batch_y = batch_y.to(DEVICE)
            optimizer.zero_grad()
            loss = criterion(model(batch_X), batch_y)
            loss.backward()
            optimizer.step()

        # Validation
        model.eval()
        with torch.no_grad():
            val_loss = criterion(model(X_test_gpu), y_test_gpu).item()

        # Early stopping — prune bad trials fast
        if val_loss < best_val_loss:
            best_val_loss   = val_loss
            patience_counter = 0
        else:
            patience_counter += 1

        if patience_counter >= EARLY_STOP_PATIENCE:
            break

        # Optuna pruning — kill clearly bad trials mid-training
        trial.report(val_loss, epoch)
        if trial.should_prune():
            raise optuna.exceptions.TrialPruned()

    return best_val_loss


# ─────────────────────────────────────────────────────────────────────────────
# Run the study
# ─────────────────────────────────────────────────────────────────────────────
if __name__ == "__main__":

    # Pruner kills bad trials early — saves a lot of time
    pruner  = optuna.pruners.MedianPruner(n_startup_trials=5, n_warmup_steps=20)
    sampler = optuna.samplers.TPESampler(seed=42)  # TPE = Bayesian optimization

    study = optuna.create_study(
        direction  = "minimize",    # we want lowest validation loss
        pruner     = pruner,
        sampler    = sampler,
        study_name = "bist100_lstm"
    )

    study.optimize(objective, n_trials=N_TRIALS, show_progress_bar=True)

    # ── Results ───────────────────────────────────────────────────────────────
    print("\n" + "="*50)
    print("BEST TRIAL")
    print("="*50)
    best = study.best_trial
    print(f"  Validation Loss : {best.value:.6f}")

    model_params   = ['seq_len', 'hidden_size', 'num_layers', 'dropout', 'lr', 'batch_size']
    feature_params = ['rsi_span', 'atr_span', 'macd_fast', 'macd_slow', 'macd_signal', 'macd_window']

    print("\n  Model Hyperparameters:")
    for key in model_params:
        print(f"    {key:15} = {best.params[key]}")

    print("\n  Feature Engineering Hyperparameters:")
    for key in feature_params:
        print(f"    {key:15} = {best.params[key]}")

    # ── Visualize results ─────────────────────────────────────────────────────
    try:
        import optuna.visualization as vis
        vis.plot_optimization_history(study).show()
        vis.plot_param_importances(study).show()
    except Exception:
        print("\nInstall plotly to see visualizations: pip install plotly")