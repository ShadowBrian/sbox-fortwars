using Sandbox;

namespace Fortwars
{
	public partial class FortwarsPlayer : Sandbox.Player
	{
		DamageInfo LastDamage;
		public Clothing.Container Clothing = new();

		public bool IsSpectator
		{
			get => Team == null;
		}

		public FortwarsPlayer()
		{
			Inventory = new Inventory( this );
		}

		public FortwarsPlayer( Client cl ) : this()
		{
			// Load clothing from client data
			Clothing.LoadFromClient( cl );
		}

		public override void Respawn()
		{
			// assign random team
			if ( Team == null )
			{
				int team = Rand.Int( 0, 1 );
				if ( team == 0 )
					Team = Game.Instance.BlueTeam;
				else
					Team = Game.Instance.RedTeam;

				// ChatBox.AddInformation( To.Everyone, $"{Name} has joined {Team.Name}", $"avatar:{Client.PlayerId}" );
			}

			SetModel( "models/citizen/citizen.vmdl" );

			// Allow Team class to dress the player
			if ( Team != null )
			{
				Team.OnPlayerSpawn( this );
			}

			if ( IsSpectator )
			{
				EnableAllCollisions = false;
				EnableDrawing = false;

				Controller = null;
				Camera = new SpectateRagdollCamera();

				base.Respawn();

				return;
			}

			Controller = new WalkController();
			Animator = new StandardPlayerAnimator();
			Camera = new FirstPersonCamera();

			EnableAllCollisions = true;
			EnableDrawing = true;

			// Draw clothes etc
			foreach ( var child in Children )
				child.EnableDrawing = true;

			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;

			Clothing.DressEntity( this );

			Inventory.DeleteContents();
			if ( Game.Instance.Round is BuildRound )
			{
				Inventory.Add( new PhysGun(), true );
			}
			if ( Game.Instance.Round is CombatRound )
			{
				Inventory.Add( new Pistol(), true );
			}

			base.Respawn();
		}

		public override void OnKilled()
		{
			base.OnKilled();

			Inventory.DropActive();

			//
			// Delete any items we didn't drop
			//
			Inventory.DeleteContents();

			BecomeRagdollOnClient( LastDamage.Force, GetHitboxBone( LastDamage.HitboxIndex ) );

			Controller = null;
			Camera = new SpectateRagdollCamera();

			EnableAllCollisions = false;

			EnableDrawing = false;

			// Don't draw clothes etc
			foreach ( var child in Children )
				child.EnableDrawing = false;
		}

		public override void Simulate( Client owner )
		{
			base.Simulate( owner );

			if ( Input.ActiveChild != null )
			{
				ActiveChild = Input.ActiveChild;
			}

			SimulateActiveChild( owner, ActiveChild );

			if ( LifeState != LifeState.Alive )
				return;

			TickPlayerUse();

			if ( Input.Pressed( InputButton.View ) )
			{
				if ( Camera is ThirdPersonCamera )
				{
					Camera = new FirstPersonCamera();
				}
				else
				{
					Camera = new ThirdPersonCamera();
				}
			}
		}

		public override void TakeDamage( DamageInfo info )
		{
			LastDamage = info;

			// hack - hitbox 0 is head
			// we should be able to get this from somewhere
			if ( info.HitboxIndex == 0 )
			{
				info.Damage *= 2.0f;
			}

			base.TakeDamage( info );

			if ( info.Attacker is FortwarsPlayer attacker && attacker != this )
			{
				// Note - sending this only to the attacker!
				attacker.DidDamage( To.Single( attacker ), info.Position, info.Damage, ((float)Health).LerpInverse( 100, 0 ) );
			}

			// TookDamage( this, info.Weapon.IsValid() ? info.Weapon.WorldPos : info.Attacker.WorldPos );
		}

		[ClientRpc]
		public void DidDamage( Vector3 pos, float amount, float healthinv )
		{
			Sound.FromScreen( "dm.ui_attacker" )
				.SetPitch( 1 + healthinv * 1 );

			// HitIndicator.Current?.OnHit( pos, amount );
		}

		[ClientRpc]
		public void TookDamage( Vector3 pos )
		{
			//DebugOverlay.Sphere( pos, 5.0f, Color.Red, false, 50.0f );

			// DamageIndicator.Current?.OnHit( pos );
		}
	}
}
