using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;



namespace TTT
{
    public enum Player { playerX, playerO }

    public enum LogSeverity { log,warn,error}

    public enum GameResult { none, xWon, oWon, draw }

    public enum GameStatus { WaitingToStart,
                                WaitingOnHuman,
                                ReadyToMove,
                                PerformingMove, 
                                ObserveMove, 
                                ObservingMove, 
                                ChangePlayer, 
                                ChangingPlayer,  
                                GiveRewards, 
                                GivingRewards , 
                                FinalObservation,
                                MakingFinalObservation,
                                EndingGame}

    public enum AgentStatus { WakingUp, Ready,Working,MadeFinalObservation,EndingGame,Resetting}

    enum MenuGroup
    {
        Default = 0,
        Sensors = 50,
        Actuators = 100
    }

    public class LogAll
    {
        private bool RealTimeLogging = false;
        private bool RealTimeLogWarningsOnly = true;
        private bool HasWarnings = false;

        private List<string> logs = new List<string>();

        public LogAll(bool realTimeLogging = false, bool realTimeLogWarningsOnly = true)
        {
            this.RealTimeLogging = realTimeLogging;
            this.RealTimeLogWarningsOnly = realTimeLogWarningsOnly;
        }

        public void Add(string log, LogSeverity level = LogSeverity.log)
        {

            int academyStep = 0;
            try
            {
                academyStep = Academy.Instance.StepCount;
            }
            catch
            {

            }
            
            string fullLog = string.Format("{0} : {1} : {2} : {3}", level, academyStep, System.Math.Round(Time.fixedTime,2), log);

            logs.Add(fullLog);

            if (level > LogSeverity.log)
            {
                HasWarnings = true;
            }

            if (RealTimeLogging)
            {
                if (level == LogSeverity.log)
                {
                    if (RealTimeLogWarningsOnly == false)
                    {
                        Debug.Log(fullLog);
                    }
                }
                else if (level == LogSeverity.warn){
                    Debug.LogWarning(fullLog);
                }
                else
                {
                    Debug.LogError(fullLog);
                }
                
            }
        }

        public void DumpLogs()
        {
            if (RealTimeLogging == false)
            {
                foreach (string l in logs)
                {
                    Debug.Log(l);
                }
                
            }
            logs = new List<string>();


        }

        public void DumpLogIfWarning()
        {
            if (RealTimeLogging == false && HasWarnings)
            {
                foreach (string l in logs)
                {
                    Debug.Log(l);
                }

            }
            logs = new List<string>();


        }

        public void ClearLogs()
        {
            logs = new List<string>();
        }
    }

}
