# Mon1ky
A tiny chess engine built for the [Sebastian Lague Tiny Chess Bot Challenge](https://github.com/SebLague/Chess-Challenge). Marked at 1912 elo.

Search: 
- Principal Variation Search (PVS)
- Quiescence Search
- Iterative Deepening
- Transposition Table

Move Ordering:
- TT Move
- MVV-LVA
- Killer Moves
- History Heuristic

Eval: 
- PeSTO Evaluation
- Tapered Evaluation
- Compressed Piece-Square Tables

Time management: 
- Adaptive Time Usage

How to use:
1. Download the API/Framework from https://github.com/SebLague/Chess-Challenge
2. Go to [the location of MyBot.cs](https://github.com/SebLague/Chess-Challenge/blob/main/Chess-Challenge/src/My%20Bot/MyBot.cs)
3. Replace the existing code with the code from Mon1ky(1010).cs
4. Build/Compile the project as Release
5. Run the executable which you will at \bin\Release
6. Play against the bot

Extra Information:
- This bot was made for fun
- The calculated ELO of this bot is 1912
- This bot is 1010 tokens(According to [Token counter](https://github.com/SebLague/Chess-Challenge/blob/main/Chess-Challenge/src/Framework/Application/Helpers/Token%20Counter/TokenCounter.cs) )
- Inspiration from Tyrant V9 was taken to create this bot
