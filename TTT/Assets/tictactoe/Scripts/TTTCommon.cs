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
        private bool DumpLogOnWarning = true;

        public bool HasWarnings { get; private set; }

        private List<string> logs = new List<string>();

        public LogAll(bool realTimeLogging = false, bool realTimeLogWarningsOnly = true, bool dumpLogOnWarning = true)
        {
            this.RealTimeLogging = realTimeLogging;
            this.RealTimeLogWarningsOnly = realTimeLogWarningsOnly;
            this.DumpLogOnWarning = dumpLogOnWarning;
            this.HasWarnings = false;
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
            
            string fullLog = string.Format("{0} : {1} : {2} : {3}", level, academyStep, System.Math.Round(Time.fixedTime,3), log);

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

            if (DumpLogOnWarning)
            {
                if (HasWarnings)
                {
                    DumpLogs();
                }
                
            }

        }

        public void DumpLogs()
        {
            if (RealTimeLogging == false)
            {

                Debug.Log("Starting Log Dump : ");
                foreach (string l in logs)
                {
                    Debug.Log(l);
                }
                Debug.Log("Ending Log Dump : ");
            }
            logs = new List<string>();
            HasWarnings = false;


        }



        public void ClearLogs()
        {
            logs = new List<string>();
            HasWarnings = false;
        }
    }

}
