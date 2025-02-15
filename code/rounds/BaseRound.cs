﻿using Sandbox;
using System;

namespace Fortwars
{
	public abstract partial class BaseRound : BaseNetworkable
	{
		public virtual int RoundDuration => 0;
		public virtual string RoundName => "";
		[Net] public float RoundEndTime { get; set; }

		public float TimeLeft
		{
			get
			{
				if ( Host.IsClient )
				{
					return RoundEndTime - Game.Instance.ServerTime;
				}

				return RoundEndTime - Time.Now;
			}
		}

		public void Start()
		{
			if ( Host.IsServer && RoundDuration > 0 )
			{
				RoundEndTime = (float)Math.Floor( Time.Now + RoundDuration );
			}

			OnStart();
		}

		public void Finish()
		{
			if ( Host.IsServer )
			{
				RoundEndTime = 0f;
			}

			OnFinish();
		}

		public virtual void OnPlayerSpawn( Player player ) { }

		public virtual void OnPlayerKilled( Player player ) { }

		public virtual void OnTick() { }

		public virtual void OnSecond()
		{
			if ( Host.IsServer )
			{
				if ( RoundEndTime > 0 && Time.Now >= RoundEndTime )
				{
					RoundEndTime = 0f;
					OnTimeUp();
				}
			}
		}

		protected virtual void OnStart() { }

		protected virtual void OnFinish() { }

		protected virtual void OnTimeUp() { }
	}
}
