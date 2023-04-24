using System.Threading;
using UnityEngine;


namespace TTT
{
    /// <summary>
    /// Main Game Loop controlls everything.
    /// </summary>
    public class TTTBoard : MonoBehaviour
    {
        // I've included a lot of over the top logging
        // The idea is to help learn the flow of events for turn based
        // Warnings only is really useful for debugging long runs as it will keep track
        // of the whole episode and dump that out in order if it got a warning.
        // if you are running headless you can see all the logging in results/[your run id]/then there will be a log for 
        // each unity environment which is really handy.
        // the log include the Academy step as well so you can see at which point the request got made.

        public bool VerboseLogging = false;
        public bool RealTimeLogging = false;
        public bool RealTimeLogWarningsOnly = false;

        // put the rewards in a scriptable object so it's easy to keep test ones around.

        public TTTRewards Rewards;

        // referance to the agents
        public TTTAgent PlayerX;

        public TTTAgent PlayerO;

        //are we training or playing
        public bool Training;

        // do we want to force the agent to keep making and learning from random
        // first moves, this is important for self training because it's really
        // easy for it to look like the agent has learnt but all it has learnt is how 
        // to defend against optimal attacks, throw a bad move at it and it won't know what to do.
        public bool PlayerXRandomFirstTurn = false;

        // just convenience for looking at self play / also good so you can see Academy steps syncing.
        public float GameRunSpeed = 1.0f;

        // no point mass creating and destroying stuff if no one looking
        // helps keep everything stable when running fast loops.
        public bool HidePieces;

        // the pieces
        public GameObject O;
        public GameObject X;


        public GameObject[] PiecePositions = new GameObject[9];


        // main game status I tried this a few ways but 
        // doing it like this was the easiest to conceptulise and manage for me.
        public GameStatus GameStatus { get; set; }

        // winners and losers etc.
        public GameResult GameResult { get; set; }

        // the turn not included in observation as can derive that
        // but do use it in reward as a scaler to make sure the reward is higher for taking
        // the win as early as possible else it will often wait to see if there's a better
        // opportunity which when playing a human means you just lost.
        public int Turn { get; private set; }

        // little logging calss i put together for this.
        public LogAll Log { get; private set; }

        // this is where the observations are taken 0 means free space 
        //1 == X 2 == O for the observations I divide by 2 so they in 0-1 space.
        public int[] BoardState { get; private set; }

        // what it says.
        public Player CurrentPlayer { get; private set; }

        // if a human has selected a piece what was it.
        public int HeuristicSelectedPiece { get; private set; }

        // for training because we have loads of loops releasing the next state I'm sure at some
        // point it will get stuck, it hasn't test but this will reset everything if it does.
        private float Timeout { get; set; }

        // Main enabler unless all the Agents are ready to go nothing happens.
        public bool AgentsWorking { get; private set; }

        // array offset from 0 for actions as we have action 0 masked as false all the time.
        private int PieceOffset = 1;

        // the in game placed pieces
        private GameObject[] PlacedPieces;

        void Start()
        {
            Log = new LogAll(RealTimeLogging, RealTimeLogWarningsOnly);

            Time.timeScale = GameRunSpeed;

            AgentsWorking = false;

        }


        // reset everything to default state and dump logs if needed.
        private void InitialiseGame()
        {
            Log.Add(CurrentPlayer + " : InitialiseGame : Start");


            if (VerboseLogging)
            {
                Log.DumpLogs();
            }
            else
            {
                Log.DumpLogIfWarning();
            }

            Timeout = 0;

            GameResult = GameResult.none;

            if (Training)
            {
                GameStatus = GameStatus.ReadyToMove;
            }
            else
            {
                GameStatus = GameStatus.WaitingToStart;
            }



            Log.ClearLogs();

            // initial board state all 0 which means all spaces available
            // 1 == playerX 
            // 2 == playerO
            BoardState = new int[9];

            //x always starts if we ever wanted to change to O starting
            //would just change graphics not code.
            CurrentPlayer = Player.playerX;

            //game start turn
            Turn = 0;

            //player selected piece if active
            HeuristicSelectedPiece = 0;


            for (int i = 0; i < 9; i++)
            {
                if (PlacedPieces != null && PlacedPieces[i] != null)
                {
                    Destroy(PlacedPieces[i]);
                }
            }

            PlacedPieces = new GameObject[9];

            Log.Add(CurrentPlayer + " : InitialiseGame : End");

        }


        /// <summary>
        /// For a real game we would have to do something different here,
        /// The human needs to be on Update which runs faster than FixedUpdate so with this example
        /// if you spam click stuff you can get unexpected behaviour for a real game we would need to separate
        /// what is required for training and what is required for runtime.  I need to think about that some more
        /// but it reinforces that any games will need to be designed with this approach in mind from the get go
        /// will be a pain to retro fit.
        /// </summary>
        void FixedUpdate()
        {

            // add overall time out
            // add move order validation checks

            if (Training)
            {
                Timeout += Time.fixedDeltaTime;

                if (Timeout > 2.0f)
                {
                    Timeout = 0.0f;

                    ResetGame();

                    Log.Add("FixedUpdate : Timed Out Resetting", LogSeverity.warn);

                }
            }


            // Main loop

            // If all agents ready initialise the game calling EndEpisode
            // will trigger this behaviour how I have it set up.

            if (PlayerO.AgentStatus == AgentStatus.Ready &&
                PlayerX.AgentStatus == AgentStatus.Ready)
            {
                PlayerO.AgentStatus = AgentStatus.Working;
                PlayerX.AgentStatus = AgentStatus.Working;
                InitialiseGame();
                AgentsWorking = true;

            }

            // Make first move then don't do anything else until that state is cleared.
            // This is First Observation->Masking->Action->Check Game State
            if (GameStatus == GameStatus.ReadyToMove && AgentsWorking)
            {
                GameStatus = GameStatus.PerformingMove;
                Timeout = 0;


                if (CurrentPlayer == Player.playerX)
                {
                    RequestDecision(PlayerX);
                }
                else
                {
                    RequestDecision(PlayerO);
                }
            }
            // Make second observation with a no action mask
            else if (GameStatus == GameStatus.ObserveMove)
            {
                GameStatus = GameStatus.ObservingMove;
                Timeout = 0;
                if (CurrentPlayer == Player.playerX)
                {
                    RequestDecision(PlayerX);
                }
                else
                {
                    RequestDecision(PlayerO);
                }
            }
            // Swap Player
            else if (GameStatus == GameStatus.ChangePlayer)
            {
                GameStatus = GameStatus.ChangingPlayer;
                Timeout = 0;
                ChangePlayer();
            }
            //End game allocate any rewards
            else if (GameStatus == GameStatus.GiveRewards)
            {
                GameStatus = GameStatus.GiveRewards;
                Timeout = 0;
                AgentsWorking = false;

                PlayerO.AgentStatus = AgentStatus.Resetting;
                PlayerX.AgentStatus = AgentStatus.Resetting;

                GiveRewards();

            }
            // Get a last Observation for both agents (I set this up initially for self play)
            // will leave it so it works like that but only the agent that is training cares about the
            // Observation.
            else if (GameStatus == GameStatus.FinalObservation)
            {
                GameStatus = GameStatus.MakingFinalObservation;
                Timeout = 0;
                RequestDecision(PlayerX);
                RequestDecision(PlayerO);
            }
            // All done reset for next loop
            else if (PlayerX.AgentStatus == AgentStatus.MadeFinalObservation &&
                    PlayerO.AgentStatus == AgentStatus.MadeFinalObservation)
            {
                Timeout = 0;
                PlayerX.AgentStatus = AgentStatus.EndingGame;
                PlayerO.AgentStatus = AgentStatus.EndingGame;
                GameStatus = GameStatus.EndingGame;

                EndGame();
            }
        }

        /// <summary>
        /// Keep in mind that RequestDecision and RequestAction are essentially 
        /// non blocking they return quick but all the have done is registered that Academy 
        /// should do something with them.  By the time it does your code will be over for this tick
        /// this is fine in most scenarios but because we are doing slow turn based we need to manage that.
        /// </summary>
        /// <param name="player"></param>
        private void RequestDecision(TTTAgent player)
        {

            Log.Add(CurrentPlayer + " : RequestDecision : Start : " + GameStatus);

            // this is non blocking but we want to wait until everything is done before
            // moving on hence all the GameStatus checks.
            player.RequestDecision();

            Log.Add(CurrentPlayer + " : RequestDecision : End : " + GameStatus);

        }

        /// <summary>
        /// Next turn change player
        /// </summary>
        private void ChangePlayer()
        {
            Log.Add(CurrentPlayer + " : ChangePlayer Current Player");
            Turn += 1;
            if (CurrentPlayer == Player.playerX)
            {
                CurrentPlayer = Player.playerO;
            }
            else
            {
                CurrentPlayer = Player.playerX;
            }
            Log.Add(CurrentPlayer + " : ChangePlayer New Player");


            if (Training)
            {
                GameStatus = GameStatus.ReadyToMove;
            }
            else
            {
                GameStatus = GameStatus.WaitingOnHuman;
            }


        }

        /// <summary>
        /// Give all the rewards and get a final observation in for all players.
        /// </summary>
        public void GiveRewards()
        {
            Log.Add(CurrentPlayer + " : GiveRewards : Start");

            if (GameResult == GameResult.xWon)
            {
                Log.Add(CurrentPlayer + " : GiveRewards : xWon");
                PlayerX.AddReward(Rewards.PlayerXWin * (Rewards.PlayerXFirstWinTurn / Turn));
                PlayerO.AddReward(Rewards.PlayerOLost);
            }
            else if (GameResult == GameResult.oWon)
            {
                Log.Add(CurrentPlayer + " : GiveRewards : OWon");
                PlayerO.AddReward(Rewards.PlayerOWin * (Rewards.PlayerOFirstWinTurn / Turn));
                PlayerX.AddReward(Rewards.PlayerXLost);
            }
            else
            {
                Log.Add(CurrentPlayer + " : GiveRewards : Draw");
                PlayerX.AddReward(Rewards.PlayerXDraw);
                PlayerO.AddReward(Rewards.PlayerODraw);
            }

            if (PlayerX.GetCumulativeReward() > Rewards.PlayerXWin)
            {
                Log.Add(CurrentPlayer + " : GiveRewards : X Rewards : Over : " + PlayerX.GetCumulativeReward(), LogSeverity.warn);
            }
            if (PlayerO.GetCumulativeReward() > Rewards.PlayerXWin)
            {
                Log.Add(CurrentPlayer + " : GiveRewards : O Rewards : Over : " + PlayerO.GetCumulativeReward(), LogSeverity.warn);
            }


            Log.Add(CurrentPlayer + " : GiveRewards : X Rewards : " + PlayerX.GetCumulativeReward());
            Log.Add(CurrentPlayer + " : GiveRewards : O Rewards : " + PlayerO.GetCumulativeReward());

            GameStatus = GameStatus.FinalObservation;

            Log.Add(CurrentPlayer + " : GiveRewards : End");
        }


        /// <summary>
        /// EndEpisode with fire the OnEpisodeBegin call back where we start the whole
        /// reset cycle.
        /// </summary>
        public void EndGame()
        {

            Log.Add(CurrentPlayer + " : EndGame : Start");

            // this will make agents revert to AgentStatus.Ready
            // once they initialize again restarting the main game loop

            PlayerX.EndEpisode();
            PlayerO.EndEpisode();


            Log.Add(CurrentPlayer + " : EndGame : End");
        }


        /// <summary>
        /// Just get legal moves that are then used for masking
        /// </summary>
        /// <returns></returns>
        public bool[] GetAvailableMoves()
        {


            bool[] availableGamePlayActions = new bool[10];

            // set true available actions (could get agent to learn these as well but another time)
            // offset by 1 as 0 is not used and 9 is no action
            for (int i = 0; i < 9; i++)
            {
                if (BoardState[i] == 0)
                {
                    availableGamePlayActions[i + 1] = true;
                }
            }

            // first action is false

            return availableGamePlayActions;
        }


        /// <summary>
        /// Update the board status
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        public bool PlacePiece(int piece)
        {

            if (!CheckValidMove(piece))
            {
                Log.Add(CurrentPlayer + " : PlacePiece : Invalid Move Attempted : " + piece, LogSeverity.warn);
                return false;

            }

            //normalise for array from action
            piece -= PieceOffset;



            // if current player is X add an X and update available moves       
            if (CurrentPlayer == Player.playerX)
            {
                if (!HidePieces)
                {
                    PlacedPieces[piece] = Instantiate(X, PiecePositions[piece].transform.position, Quaternion.identity);
                }

                BoardState[piece] = 1;
            }
            // if current player is O add an O and update available moves  
            else if (CurrentPlayer == Player.playerO)
            {
                if (!HidePieces)
                {
                    PlacedPieces[piece] = Instantiate(O, PiecePositions[piece].transform.position, Quaternion.identity);
                }

                BoardState[piece] = 2;
            }

            return true;
        }

        /// <summary>
        /// Is the move allowed
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        public bool CheckValidMove(int piece)
        {
            //normalise for array

            piece -= 1;


            return BoardState[piece] == 0;
        }


        /// <summary>
        /// Unexpected error reset, EpisodeInterrupted() is the best I could find
        /// but the data still gets taken into account for training it's saying
        /// the Agent Ended early but not it's fault.  Ideally I want something that says
        /// it all caught fire disregard this episode but couldn't find a method to do that.
        /// for larger datasets this will get lost in general noise but we are dealing with tiny 
        /// sample sizes.
        /// </summary>
        public void ResetGame()
        {
            Log.Add(CurrentPlayer + " : ResetGame", LogSeverity.warn);

            AgentsWorking = false;

            PlayerO.EpisodeInterrupted();
            PlayerX.EpisodeInterrupted();
        }


        /// <summary>
        /// If a human started teh game
        /// </summary>
        public void StartHumanGame()
        {
            InitialiseGame();
            GameStatus = GameStatus.WaitingOnHuman;
        }

        /// <summary>
        /// AI Making a move but in the context fo playing a human ot it's self
        /// not training.
        /// </summary>
        public void MakeAIMove()
        {
            Log.Add(CurrentPlayer + " : MakeAIMove : Making Move Start");
            //This assumes the check has been made that this player
            //is running from a brain and not training WriteDiscreteActionMask will get
            //get called instead of heuristic and get a decision.
            //will get set back to waiting on human (or game end)


            GameStatus = GameStatus.ReadyToMove;
            Log.Add(CurrentPlayer + " : MakeAIMove : Making Move End");
        }

        /// <summary>
        /// Human making a move
        /// </summary>
        /// <param name="piece"></param>
        public void MakeHeuristicMove(int piece)
        {
            piece += PieceOffset;
            if (CheckValidMove(piece))
            {
                HeuristicSelectedPiece = piece;
                GameStatus = GameStatus.ReadyToMove;
                Log.Add(CurrentPlayer + " : MakeHeuristicMove : Making Move (+1) : " + piece);
            }
            else
            {
                Log.Add(CurrentPlayer + " : MakeHeuristicMove : Not valid move (+1) : " + piece, LogSeverity.warn);
            }
        }


        /// <summary>
        /// win, loose, draw or next turn.
        /// </summary>
        /// <returns></returns>
        public GameResult CheckGameStatus()
        {

            int winner = 0;

            // Must be nicer way to do this but check if there are
            // any winning lines

            // checking for any matching rows that are not matching 0's

            int[,] twoDArr = new int[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    twoDArr[i, j] = BoardState[i * 3 + j];
                }
            }

            // Check rows
            for (int i = 0; i < 3; i++)
            {
                if (twoDArr[i, 0] == twoDArr[i, 1] && twoDArr[i, 1] == twoDArr[i, 2])
                {

                    if (twoDArr[i, 0] != 0)
                    {
                        winner = twoDArr[i, 0];
                    }
                }
            }

            // Check columns
            for (int j = 0; j < 3; j++)
            {
                if (twoDArr[0, j] == twoDArr[1, j] && twoDArr[1, j] == twoDArr[2, j])
                {

                    if (twoDArr[0, j] != 0)
                    {
                        winner = twoDArr[0, j];
                    }
                }
            }

            // Check diagonals
            if (twoDArr[0, 0] == twoDArr[1, 1] && twoDArr[1, 1] == twoDArr[2, 2])
            {
                if (twoDArr[0, 0] != 0)
                {
                    winner = twoDArr[0, 0];
                }


            }
            if (twoDArr[0, 2] == twoDArr[1, 1] && twoDArr[1, 1] == twoDArr[2, 0])
            {

                if (twoDArr[0, 2] != 0)
                {
                    winner = twoDArr[0, 2];
                }
            }

            // X wins
            if (winner == 1)
            {
                return GameResult.xWon;
            }
            // O wins
            else if (winner == 2)
            {
                return GameResult.oWon;

            }
            // all turns used and no winner
            else if (winner == 0 && Turn == 8)
            {
                return GameResult.draw;
            }
            //still playing
            else
            {
                return GameResult.none;
            }
        }

    }
}

