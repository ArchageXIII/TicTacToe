# TicTacToe
ML-Agents simple example of turn based unity game for learning

Turned out to be more convoluted that I was expecting to get working.  The code is over commented and lots of logging but the idea is for anyone to be able to work through it and get an idea of the flow of events and challenges rather than making a game but you can play it as well.

Feel free to grab the code and improve it, I couldn't find any nice turn based examples when I was looking which is why I did this.

# Video
I ramble on a bit, it's not a training guide more of a discussion! but go over the project setup and code a bit.

[![Video Title](https://img.youtube.com/vi/FAzSe-eBN8c/0.jpg)](https://www.youtube.com/watch?v=FAzSe-eBN8c)

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

## Game Play

If you just want to play the game there is a PlayGame scene where you can play against one of the trained brains, it's got lots of logging turned on by default if you want to see the flow.

Click Reset if you want to change which agent to play or set them to play each other.

## Training config files
I've included the config files I used they can be tuned better I am sure, theres loads of comments in them here [MLAgentsConfig](https://github.com/ArchageXIII/TicTacToe/tree/main/MLAgentsConfig)

## Observations
This was the biggest mistake I make I initially set the observations up as 9 one for each space so both agents could see the same board with 0 for empty, 1 for playerX 2 for playerO normalized to 0.5 and 1 for the observation.
This sort of worked which was the issue and I spent way to long fiddling with settings, rewards etc. to get it to converge and play well.  It kept missing obvious wins and blocks even tho it looked like it had trained ok.

The training was having a hard time figuring out that 0.5 was X and 1 was O but was close.

I ended up flattening out the board into 18 observations so 9 observations for X with 0 for empty and 1 for X and another 9 observations with 0 for empty and 1 for O which I believe is referred to as one-hot encoding.  Once I had it set up like this everything started working, I didn't have to have overly complicated reward structure or lead the learning as much as I was.


## Rewards
* Win = 1
* Lose = -1
* PlayerX Draw = 0
* PlayerO Draw = 0 

 I left these in from when I had the observations set incorrectly and was trying different approaches, it's interesting to see the effects of fiddling with them but with the default example training file I have included there is no need.  The training converges on 0 reward as that is the only outcome if both players don't make a mistake.

* Missed opportunity to win = 0
* Missed opportunity to block = 0

## General Training

Very basic guide [here](./docs/setuptraining.md) and I talk about it in the video.










