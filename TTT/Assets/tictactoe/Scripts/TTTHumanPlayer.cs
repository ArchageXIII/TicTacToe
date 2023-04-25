using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.Rendering;



namespace TTT
{
    public class TTTHumanPlayer : MonoBehaviour
    {
        /// <summary>
        /// Just simple script to let us play the game with some feedback
        /// </summary>

        public TTTBoard Board;

        public TextMeshPro playerOText;

        public TextMeshPro playerXText;

        public TextMeshPro playerStarted;


        // Start is called before the first frame update
        void Start()
        {
            SetPlayerText();

        }


        private void SetPlayerText()
        {
            if (Board.PlayerX.BehaviourParameters.BehaviorType == BehaviorType.HeuristicOnly)
            {
                playerXText.text = "PlayerX\nHeuristics";
                playerXText.color = Color.white;
            }
            else
            {
                playerXText.text = "PlayerX\nInference";
                playerXText.color = Color.blue;
            }
            if (Board.PlayerO.BehaviourParameters.BehaviorType == BehaviorType.HeuristicOnly)
            {
                playerOText.text = "PlayerO\nHeuristics";
                playerOText.color = Color.white;
            }
            else
            {
                playerOText.text = "PlayerO\nInference";
                playerOText.color = Color.blue;
            }
        }





        // Update is called once per frame
        void Update()
        {


            
            // if agent is running and not heuristic
            if (Board.AgentsWorking && 
                Board.PlayerX.BehaviourParameters.BehaviorType != BehaviorType.HeuristicOnly)
            {
                if (Board.CurrentPlayer == Player.playerX &&
                    Board.GameStatus == GameStatus.WaitingOnHuman)
                {
                    Board.MakeAIMove();
                }
            }

            if (Board.AgentsWorking &&
                Board.PlayerO.BehaviourParameters.BehaviorType != BehaviorType.HeuristicOnly)
            {
                if (Board.CurrentPlayer == Player.playerO && 
                    Board.GameStatus == GameStatus.WaitingOnHuman)
                {
                    Board.MakeAIMove();
                }
            }

            if (Board.GameStatus == GameStatus.WaitingToStart)
            {
                playerStarted.color = Color.cyan;

            }
            else
            {
                playerStarted.color = Color.blue;
            }






            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.transform.CompareTag("reset"))
                    {
                        Board.ResetGame();
                    }
                    else if (Board.GameStatus == GameStatus.WaitingToStart && hit.transform.CompareTag("start"))
                    {
                        Board.StartHumanGame();
                    }
                    else if (hit.transform.CompareTag("playerX"))
                    {
                        if (Board.GameStatus == GameStatus.WaitingToStart)
                        {

                            if (Board.PlayerX.BehaviourParameters.BehaviorType == BehaviorType.HeuristicOnly)
                            {

                                if (Board.PlayerX.BehaviourParameters.Model != null)
                                {
                                    Board.PlayerX.BehaviourParameters.BehaviorType = BehaviorType.InferenceOnly;
                                }
                                else
                                {
                                    Debug.LogWarning("No Model loaded in playerX");
                                }

                            }
                            else
                            {
                                Board.PlayerX.BehaviourParameters.BehaviorType = BehaviorType.HeuristicOnly;
                            }
                            SetPlayerText();
                        }
                        else
                        {
                            Debug.LogWarning("Reset game first");
                        }

                    }
                    else if (hit.transform.CompareTag("playerO"))
                    {

                        if (Board.GameStatus == GameStatus.WaitingToStart)
                        {
                            if (Board.PlayerO.BehaviourParameters.BehaviorType == BehaviorType.HeuristicOnly)
                            {

                                if (Board.PlayerO.BehaviourParameters.Model != null)
                                {
                                    Board.PlayerO.BehaviourParameters.BehaviorType = BehaviorType.InferenceOnly;
                                }
                                else
                                {
                                    Debug.LogWarning("No Model loaded in playerO");
                                }

                            }
                            else
                            {
                                Board.PlayerO.BehaviourParameters.BehaviorType = BehaviorType.HeuristicOnly;
                            }
                            SetPlayerText();
                        }
                        else
                        {
                            Debug.LogWarning("Reset game first");
                        }

                    }
                    else if (Board.GameStatus == GameStatus.WaitingOnHuman)
                    {
                        int piece;
                        // get the area name
                        bool foundPiece = int.TryParse(hit.transform.name, out piece);



                        if (foundPiece)
                        {
                            Board.MakeHeuristicMove(piece);
                        }
                    }
                }
            }
        }
    }
}
 
