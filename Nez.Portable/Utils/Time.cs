using System;
using System.Runtime.CompilerServices;


namespace Nez
{
	/// <summary>
	/// provides frame timing information
	/// </summary>
	public static class Time
	{
		/// <summary>
		/// total time the game has been running
		/// </summary>
		public static float TotalTime => IsFixedUpdate ? throw new NotImplementedException() : totalTime;
		static float totalTime;

		/// <summary>
		/// delta time from the previous frame to the current, scaled by timeScale
		/// </summary>
		public static float DeltaTime => UnscaledDeltaTime * TimeScale;

		/// <summary>
		/// the fixed time step in seconds
		/// </summary>
		public static float FixedTimeStep = 1f/30f;

		/// <summary>
		/// unscaled version of deltaTime. Not affected by timeScale
		/// </summary>
		public static float UnscaledDeltaTime => IsFixedUpdate ? FixedTimeStep : dt;

		/// <summary>
		/// total time since the Scene was loaded
		/// </summary>
		public static float TimeSinceSceneLoad;

		/// <summary>
		/// time scale of deltaTime
		/// </summary>
		public static float TimeScale = 1f;

		/// <summary>
		/// total number of frames that have passed
		/// </summary>
		public static uint FrameCount;

		/// <summary>
		/// is the game currently processing a fixed update
		/// </summary>
		public static bool IsFixedUpdate {get; private set;}

		static float dt;


		internal static void Update(float dt)
		{
			Time.dt = dt;
			totalTime += dt;
			TimeSinceSceneLoad += dt;
			FrameCount++;
		}

		public static void EnterFixedUpdate() {
			IsFixedUpdate = true;
		}
		public static void ExitFixedUpdate() {
			IsFixedUpdate = false;
		}


		internal static void SceneChanged()
		{
			TimeSinceSceneLoad = 0f;
		}


		/// <summary>
		/// Allows to check in intervals. Should only be used with interval values above deltaTime,
		/// otherwise it will always return true.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CheckEvery(float interval)
		{
			// we subtract deltaTime since timeSinceSceneLoad already includes this update ticks deltaTime
			return (int) (TimeSinceSceneLoad / interval) > (int) ((TimeSinceSceneLoad - DeltaTime) / interval);
		}
	}
}