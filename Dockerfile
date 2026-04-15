FROM python:3.10-slim

WORKDIR /app

COPY requirements.txt .

# CPU-only torch keeps the image small; switch index URL for GPU builds.
RUN pip install --no-cache-dir torch --index-url https://download.pytorch.org/whl/cpu
RUN pip install --no-cache-dir -r requirements.txt

COPY api/ ./api/
COPY src/ ./src/
COPY models/ ./models/

EXPOSE 8000

CMD ["uvicorn", "api.fast_api_server:app", "--host", "0.0.0.0", "--port", "8000"]
