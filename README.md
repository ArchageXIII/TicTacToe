# TicTacToe
ML-Agents simple example of turn based unity game for learning

Turned out to be more convoluted that I was expecting to get working.  The code is over commented and lots of logging but the idea is for anyone to be able to work through it and get an idea of the flow of events and challenges rather than making a game but you can play it as well.

Feel free to grab the code and improve it, I couldn't find any nice turn based examples when I was looking which is why I did this.

# Video
I ramble on a bit, it's not a training guide more of a discussion!
<iframe width="560" height="315" src="https://www.youtube.com/embed/XY5Qqp-CIzk" frameborder="0" allow="accelerometer; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

# Installation

You have installed and run some of the demo projects from Unity's [getting started guide](https://github.com/Unity-Technologies/ml-agents/blob/release_20_docs/docs/Getting-Started.md)

This is not an installation guide but here are a few notes from when I installed ml-agents on windows over and above the unity guide which you should follow.

* Install [miniconda](https://docs.conda.io/en/latest/miniconda.html) everything machine learning I've been working with is very version dependent so I would suggest installing exactly the packages and version you want to run.
* I used python 3.8
* conda create -n unity-ml python=3.8
* conda activate unity-ml
* git clone --branch release_20 https://github.com/Unity-Technologies/ml-agents.git
* in unity install the ml-agents package 2.0
* then in unity install the extensions you pulled from git (ml-agents\com.unity.ml-agents.extensions\package.json)
* pip3 install torch~=1.7.1 -f https://download.pytorch.org/whl/torch_stable.html
* python -m pip install mlagents==0.30.0
* I had to downgrade protobuf 
* pip install --upgrade "protobuf<=3.20.1"
* if all went well malgents-learn -- help will work.

# Structure

## Unity project
The Unity project files are all in [TTT](https://github.com/ArchageXIII/TicTacToe/tree/main/TTT)
Only packages are mlagents and text mesh pro which is included
It's a URP project not that theres anything in there it's just what I tend to use.

All the files are under tictactoe loads of comments in the source and if you turn on logging there is loads of logging but it's designed to show the flow of events.

## Training config files
I've included the config files I used they can be tuned better I am sure, theres loads of comments in them here [MLAgentsConfig](https://github.com/ArchageXIII/TicTacToe/tree/main/MLAgentsConfig)

## Rewards
* Win = 1
* Lose = -1
* Draw = 0
* Missed opportunity to win = -0.5 (but keep playing giving -0.5 for each chance missed)

It took me a long time to come up with this, I tried various strategies for getting the Agent to take
the fist available win but including turns, rewards that scaled the earlier a win happened, really scaling
back gamma in the training nothing seemed to influence the agent enough to not wait for better rewards later
which is fine for a lot fo games but not for this.

Giving a negative reward but continuing to play really seemed to help, I had avoided doing that to start with as I didn't want to over prescribe it's actions but if I didn't it never got to a point where it consistently took the opportunity as soon as it came up.

## General Training

Very basic guide [here](./docs/setuptraining.md) and I talk about it in the video.

I trained the agents one at a time against each other because I was having issues with self training returning incorrect actions when training 2 together at a time.  Details in code comments.

I found it really important to force X to train from random start locations even if they were sub optimal because when you later use it to train PlayerO if you don't player O only learns to play against optimal moves and if you play badly as PlayerX you tend to win which is not what we want.

General training flow was PlayerX taking a random first move against PlayerO heuristics just giving random valid responses until Player X got the hang of it 3m steps but could have gone for longer.  I need to think about a strategy for initial training for more complex games.

PlayerO the same training against random moves from X for 3m steps.

Then PlayerX against trained PlayerO, Player O against that Trained PlayerX

Back and forth doing that using the last training set to initialize the next set until I got good coverage.

I need to think of a way to automate that if self play is going to be flaky, should be able to batch it some how I'm sure.









