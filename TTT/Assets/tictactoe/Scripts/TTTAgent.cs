using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

using Unity.MLAgents.Sensors.Reflection;
using System.Linq;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

namespace TTT
{
    public class TTTAgent : Agent
    {
        /// <summary>
        /// Agent Overrides, an Academy instance gets set up automatically this is a singleton you can access it via Academy.Instance
        /// 
        /// By default it ticks once per FixedUpdate you can see this by looking at Academy.Instance.StepCount how it aligns, the frequency
        /// scales with Time.timeScale so it stays in sync so for training even if it's not physics best to do it off Fixed Update.
        /// 
        /// Academy is the layer between python and unity the key takeaway is when you do a RequestDecision or RequestAction that essentially signals
        /// the the Agent is ready to do that so it won't get picked up until the Academy service checks which will be the tick after or later.
        /// this means that if you have code like turn based where you need to get everything done then move to the next turn you have to implement a strategy
        /// where you wait for the request to finish before moving on.  That's what TTTBoard does there's probably a nicer way to do it but this works.
        /// 
        /// See main mage for details of the flow of events but I've laid the methods out here pretty much in order and if you turn on logging you can see how it works real time.
        /// 
        /// </summary>



        // which player the agent represents, the Agents are trained separately
        // because it's a unbalanced game PlayerO has one less move and can't ever win if PlayerX plays a perfect game.
        public Player Player;

        // reference to the Board this controls the flow of events.
        public TTTBoard Board;

        // the vector sensor component (but not the actual sensor)
        // using a custom vector, I just copied the VectorSensor and tweaked it a bit
        // could use internal but there's not ang great documentation on how to do a custom one so wanted to test
        // implementing one.  You use this to get a handle on the sensor but see notes later about that.
        public TTTVectorSensorComponent VSC { get; set; }

        // status of the Agent I use the Agents being both ready to start the initialisation each episode to keep
        // everything in sync.
        public AgentStatus AgentStatus { get; set; }

        // just so I can get access to if the Agent has a Brain or not for the human and self play.
        public BehaviorParameters BehaviourParameters { get; private set; }


        private void Start()
        {
            //Gets called before initialize
            VSC = GetComponent<TTTVectorSensorComponent>();
            BehaviourParameters = GetComponent<BehaviorParameters>();
            AgentStatus = AgentStatus.WakingUp;

        }

        /// <summary>
        /// Treat this like Start() it gets called after Start tho.
        /// </summary>
        public override void Initialize()
        {
            
            AgentStatus = AgentStatus.Ready;
        }





        /// <summary>
        /// This gets called every time an episode gets reset or the first time it starts.
        /// Gets called by Academy not you directly.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            Board.Log.Add("Agent Player : " +Player+ " : OnEpisodeBegin : ");
            AgentStatus = AgentStatus.Ready;
        }


        

        /// <summary>
        /// sensor is the internal sensor we are not using that (although in this basic example we could)
        /// but I wanted to test custom sensor.  You still need to do your custom sensors in here to ge the timing right
        /// Academy calls this not you, if you call it manually it all gets confused and you over fill the observations.
        /// To trigger this collection you need to RequestDecision or RequestAction so if you just need to do an observation
        /// with no action I ended up creating a dummy no action discrete action that I just fire if I want an observation.
        /// </summary>
        /// <param name="sensor"></param>
        public override void CollectObservations(VectorSensor sensor)
        {
            Board.Log.Add("Agent Player : " + Player + " : CollectObservations : " + Board.GameStatus);

            foreach (int value in Board.BoardState)
            {
                // Not sure why we have to call GetSensor() each time but if you do not everything gets out
                // Of sync and weird, I'm guessing that behind the scenes Academy is perhaps resetting the sensors
                // Each episode and if you have a reference to it you no longer have the current sensor 
                // Something like that.
                
                VSC.GetSensor().AddObservation((float)value / 2);

            }

            Board.Log.Add("Agent Player : " + Player + " : CollectObservations : " + VSC.GetSensor().GetObservations(true));
        }

        /// <summary>
        /// This is where you can control what actions are available depending on the gameplay state or
        /// you could just reward negative for taking the wrong action, I went with masking.
        /// If you train a brain with masking really you need to leave it in there after as that's what it's used to with the training.
        /// 
        /// Note: training two agents learning against each other can give unpredictable results you sometimes get a null back
        /// which looking at the code gets changes to a 0 which could be mapped to one of your actions.
        /// Because of this I have left 0 masked all the time then if I get a ) I know there was an error.
        /// For training an agent against heuristics or inference with a brain it works fine.
        /// 
        /// If you mask all the actions you will get an error if you want no action then keep one of the actions
        /// as its function being no action and just leave that unmasked.
        /// 
        /// Note. Heuristics will ignore this you need to replicate the functionality in Heuristics example below
        /// it's worth getting Heuristics to work as close to the training model as possible to aid debugging and
        /// if you want to record actions for training etc.
        /// </summary>
        /// <param name="actionMask"></param>
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {

            Board.Log.Add("Agent Player : " + Player + " : WriteDiscreteActionMask : " + Board.GameStatus);

            //0 is always false if it is ever true training had got out of sync
            //1-9 are the legal moves
            //10 is no action / observe only, tempting to put on 0 but I would rather see the issue than mask it.

            // get available actions
            bool[] availableGamePlayActions = Board.GetAvailableMoves();

            if (Board.GameStatus == GameStatus.PerformingMove)
            {
                if (Board.PlayerX && Board.Turn == 0)
                {

                    for (int i = 1; i < 10; i++)
                    {
                        actionMask.SetActionEnabled(0, i, false);
                    }

                    int rnd = UnityEngine.Random.Range(1, 10);
                    actionMask.SetActionEnabled(0, 0, false);
                    actionMask.SetActionEnabled(0, rnd, true);
                    actionMask.SetActionEnabled(0, 10, false);

                }
                else
                {
                    // set available actions
                    // remember this wont get called if running heuristics so need to do additional check 
                    // in on action received for that

                    //0 and 10 false no action
                    for (int i = 1; i < availableGamePlayActions.Count(); i++)
                    {
                        actionMask.SetActionEnabled(0, 0, false);
                        actionMask.SetActionEnabled(0, i, availableGamePlayActions[i]);
                        actionMask.SetActionEnabled(0, 10, false);
                    }
                }
            }
            else if (Board.GameStatus == GameStatus.ObservingMove || Board.GameStatus == GameStatus.MakingFinalObservation)
            {

                actionMask.SetActionEnabled(0, 0, false);
                for (int i = 1; i < 10; i++)
                {
                    actionMask.SetActionEnabled(0, i, false);
                }
                // set 10 to true no action required, keeping that consistent through out training.
                actionMask.SetActionEnabled(0, 10, true);
            }
            else
            {
                Debug.Assert(false, "Agent Player : " + Player + " : WriteDiscreteActionMask : Should Never Get Here");
                Board.Log.Add("Agent Player : " + Player + " : WriteDiscreteActionMask : Should Never Get Here",LogSeverity.error);
            }




            Board.Log.Add("Agent Player : " + Player + " : WriteDiscreteActionMask : End");

        }
        /// <summary>
        /// Spend the time getting this to be representative fo the training if you are running Heuristics
        /// or the agent is set as default with no brain this gets called. You can see below if its training
        /// I return a random legal move if it's human the move it what they pick.
        /// </summary>
        /// <param name="actionsOut"></param>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            Board.Log.Add("Agent Player : " + Player + " : Heuristic : " + Board.GameStatus);

            var discreteActionsOut = actionsOut.DiscreteActions;

            if (Board.GameStatus == GameStatus.PerformingMove)
            {
                if (Board.Training)
                {
                    bool[] availableActions = Board.GetAvailableMoves();

                    List<int> trueIndices = new List<int>();

                    // don't include 0 

                    for (int i = 1; i < 10; i++)
                    {
                        if (availableActions[i])
                        {
                            trueIndices.Add(i);
                        }
                    }


                    int randomPiece = trueIndices[UnityEngine.Random.Range(0, trueIndices.Count)];

                    Board.Log.Add("Agent Player : " + Player + " : Heuristic : randomly picked : " + randomPiece);


                    discreteActionsOut[0] = randomPiece;
                }
                else
                {

                    Board.Log.Add("Agent Player : " + Player + " : Heuristic : player picked : " + (Board.HeuristicSelectedPiece));

                    //offset as 0 is no action
                    discreteActionsOut[0] = Board.HeuristicSelectedPiece;
                }
            }
            else if (Board.GameStatus == GameStatus.ObservingMove || Board.GameStatus == GameStatus.MakingFinalObservation)
            {
                discreteActionsOut[0] = 10;
            }
            else
            {
                Debug.Assert(false, "Agent Player : " + Player + " : Heuristic : Should Never Get Here");
                Board.Log.Add("Agent Player : " + Player + " : Heuristic : Should Never Get Here", LogSeverity.error);
            }



            Board.Log.Add("Agent Player : " + Player + " : Heuristic : End");

        }
        /// <summary>
        /// Finally everything comes here this is where you take any actions
        /// Ive split rewards and observations into separate steps on subsequent Academy steps
        /// so it comes through this twice each player each turn 
        /// Observer -> Get Action ->Make move -> check outcome
        /// If not end game do a no action loop around again for another Observer then move to the other player
        /// If it is end game run end game which awards rewards then invokes another round of observation for both players
        /// Then ends the session and resets everything.
        /// </summary>
        /// <param name="actions"></param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            var action = actions.DiscreteActions[0];
            Board.Log.Add("Agent Player : " + Player + " : OnActionReceived : " + Board.GameStatus);

            bool placedPiece = false;


            if (action > 0 && action < 10)
            {

                bool couldWinThisTurn = Board.CheckCouldWinOnNextMove(Player);

                Board.Log.Add("Agent Player : " + Player + " : OnActionReceived : Could Win : " + couldWinThisTurn);


                placedPiece = Board.PlacePiece(action);
                if (!placedPiece)
                {
                    Board.Log.Add("Agent Player : " + Player + " : OnActionReceived : Failed to place piece");
                    Board.ResetGame();

                }
                else
                {

                    
                    
                    Board.Log.Add("Agent Player : " + Player + " : OnActionReceived : Placed Piece : " + action);
                    Board.GameResult = Board.CheckGameStatus();

                    if (Board.GameResult == GameResult.none)
                    {
                        // if we could have won but didn't give negative reward
                        // but let game play carry on and not illegal move.
                        if (couldWinThisTurn)
                        {
                            AddReward(Board.Rewards.CouldHaveWon);
                            Board.Log.Add("Agent Player : " + Player + " : OnActionReceived : Could Win but did not : " + GetCumulativeReward());

                        }
                        
                        Board.GameStatus = GameStatus.ObserveMove;
                    }
                    else
                    {
                        //its end game state start end game process.
                        Board.GameStatus = GameStatus.GiveRewards;
                    }
                }
            }
            else if (action == 10){
                //do nothing observation only

                if (Board.GameStatus == GameStatus.ObservingMove)
                {
                    Board.Log.Add("Agent Player : " + Player + " : OnActionReceived : Took No Action Observation Only swap to next player");
                    Board.GameStatus = GameStatus.ChangePlayer;
                }
                else if (Board.GameStatus == GameStatus.MakingFinalObservation)
                {
                    Board.Log.Add("Agent Player : " + Player + " : OnActionReceived : Took No Action Observation End Game");

                    AgentStatus = AgentStatus.MadeFinalObservation;
                }


            }
            else
            {
                Board.Log.Add("Agent Player : " + Player + " : OnActionReceived : Invalid action, received : " + action);
                Board.ResetGame();
            }
            Board.Log.Add("Agent Player : " + Player + " : OnActionReceived : End");
        }

    }

}

