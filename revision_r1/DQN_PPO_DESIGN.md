# DQN / PPO baseline design (scaffold only)
* Env: wrap World+Robot headless as a step API exposed by a `--gym-server` stdin/stdout JSON loop. Python then uses a gymnasium.Env (obs = last L feel codes one-hot (8*L) + goal bearing; action = 8 relative moves; reward = -energy step cost, +100 goal, -50 timeout at 2000 steps).
* Protocol: train on the 3 non-held-out terrains (the same LOO), using the same seeds 101..1010 and the same episode-seed mixing (seed+777); evaluate 30 runs on the held-out terrain.
* Budget: match teacher experience (250 attempts x mean steps) and also report a 10x budget. Use SB3 DQN/PPO with default hyperparameters plus a small grid; report the best on validation terrains only.
* Status: not implemented. The C# gym-server is the main cost (~1-2 days).
