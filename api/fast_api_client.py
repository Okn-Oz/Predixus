import requests

url = "http://localhost:8000/predict"

file_path = "all_bist100.csv" 

with open(file_path, "rb") as f:
    files = {"file": (file_path, f, "text/csv")}
    response = requests.post(url, files=files)

print("Status Code:", response.status_code)
print("Response:", response.json())