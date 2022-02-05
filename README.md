# SoD2-Skill-Reroll 2
A program that automatically re-rolls survivor traits in State of Decay 2 until the desired traits are present. Uses screenshots and a screen reader to determine what skills are currently shown on screen. It is an improvement on Benjamin Noer's work (https://github.com/BenjaminNoer/SoD2-Skill-Reroll) for traits only. The search for skills have been disabled.

This project uses the Tesseract library (https://tesseract.patagames.com/) and Input Simulator (https://archive.codeplex.com/?p=inputsimulator).

This program takes screenshots of the game window where survivors' traits appear and uses a screen reader to determine when the desired trait is present. The program automatically gives virtual keybaord inputs (arrow keys and enter) to press the UI buttons.

Before you start:

This tool does not speed up the process very much but it does automate it. For a single trait it takes about 500 rolls (about 4 minutes)  for an 80% chance of finding a hit. When wanting to find 2 traits at once, the chances for finding those traits at a time shoot up (or down) to 0,000599% - which means about 270000 rolls (about 1,5 days) of nonstop rerolling.

Instructions:

1. To run the program, download the .zip file from the latest release (https://github.com/BenjoKazooie/SoD2-Skill-Reroll/releases) and extract the files. Make sure they are all in the same folder and run "SoD2 Reroll.exe".
2. Pick any traits you want to search for in the dropdon boxes or leave any blank to not search for any traits for that survivor. 
3. The numeric field ith the label 'wait after start (seconds)' refers to the number of seconds the program will wait after pressing start before giving any inputs. This is to give time to tab into the game. Make sure the game is open and ready to reroll survivorrs before starting the program.
4. Choose the resolution that your game is running in so the program knows where to look for skills as the screen reader will not look at the whole screen.
5. Hold the control button at any time to stop the program when it is searching otherwise it will keep giving virtual inputs.
