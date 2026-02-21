import oracledb

conn = oracledb.connect(
    user="legal_minds",
    password="MyPass123",
    dsn="localhost:1521/xepdb1"
)

print("Connected Successfully!")