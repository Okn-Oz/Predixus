import torch

if torch.cuda.is_available():
    DEVICE = torch.device("cuda")
    print(f"Hardware setup: Using GPU ({torch.cuda.get_device_name(0)})")
else:
    DEVICE = torch.device("cpu")
    print("Hardware setup: Using CPU")