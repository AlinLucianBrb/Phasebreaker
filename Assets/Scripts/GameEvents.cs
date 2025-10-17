using System;

public static class GameEvents
{
    // Example events
    public static event Action<int> OnBallLost;
    public static event Action OnBallStick;
    public static event Action<float> OnBallLaunch;


    // Call this to trigger the event
    public static void BallLost(int amount) => OnBallLost?.Invoke(amount);
    public static void BallStick() => OnBallStick?.Invoke();
    public static void BallLaunch(float angle) => OnBallLaunch?.Invoke(angle);
}
