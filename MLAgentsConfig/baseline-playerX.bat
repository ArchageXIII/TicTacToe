@echo off
tasklist | find /i "tictactoe.exe" >nul
if errorlevel 1 (
    echo No instances of tictactoe.exe found.
) else (
    taskkill /f /im tictactoe.exe
)
start /wait "" "mlagents-learn" "baseline.playerX.yaml" "--run-id=baseline-playerX_Round1" "--seed=10"  "--env=D:\TicTacToe\Build" "--num-envs=10" "--no-graphics" "--time-scale=20"
