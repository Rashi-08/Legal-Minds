from fastapi import FastAPI
from auth import get_current_user
from fastapi import Depends
from database import engine, Base
from auth import router
from fastapi.middleware.cors import CORSMiddleware

app = FastAPI()

Base.metadata.create_all(bind=engine)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:63343"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(router)

@app.get("/")
def root():
    return {"message": "Legal Minds API Running"}

@app.get("/protected")
def protected_route(user=Depends(get_current_user)):
    return{
        "message": "You are authenticated",
        "user":user
    }
