using System.Collections.Generic;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using Microsoft.Xna.Framework;


namespace Nez.Farseer
{
	public class FSRigidBody : Component
	{
		public Body Body;
 
		FSBodyDef _bodyDef = new FSBodyDef();
		bool _ignoreTransformChanges;
		internal List<FSJoint> _joints = new List<FSJoint>();

		// TODO: memory concern here
		List<CollisionEventInfo> collisionEvents = new List<CollisionEventInfo>();
		public IEnumerable<CollisionEventInfo> CollisionEvents => collisionEvents;
		List<SeparationEventInfo> separationEvents = new List<SeparationEventInfo>();
		public IEnumerable<SeparationEventInfo> SeparationEvents => separationEvents;

		public IEnumerable<ContactInfo> ContactList => EnumerateContactList();

		#region Configuration

		public FSRigidBody SetBodyType(BodyType bodyType)
		{
			if (Body != null)
				Body.BodyType = bodyType;
			else
				_bodyDef.BodyType = bodyType;
			return this;
		}


		public FSRigidBody SetLinearVelocity(Vector2 linearVelocity)
		{
			if (Body != null)
				Body.LinearVelocity = linearVelocity;
			else
				_bodyDef.LinearVelocity = linearVelocity;
			return this;
		}


		public FSRigidBody SetAngularVelocity(float angularVelocity)
		{
			if (Body != null)
				Body.AngularVelocity = angularVelocity;
			else
				_bodyDef.AngularVelocity = angularVelocity;
			return this;
		}


		public FSRigidBody SetLinearDamping(float linearDamping)
		{
			if (Body != null)
				Body.LinearDamping = linearDamping;
			else
				_bodyDef.LinearDamping = linearDamping;
			return this;
		}


		public FSRigidBody SetAngularDamping(float angularDamping)
		{
			if (Body != null)
				Body.AngularDamping = angularDamping;
			else
				_bodyDef.AngularDamping = angularDamping;
			return this;
		}


		public FSRigidBody SetIsBullet(bool isBullet)
		{
			if (Body != null)
				Body.IsBullet = isBullet;
			else
				_bodyDef.IsBullet = isBullet;
			return this;
		}


		public FSRigidBody SetIsSleepingAllowed(bool isSleepingAllowed)
		{
			if (Body != null)
				Body.IsSleepingAllowed = isSleepingAllowed;
			else
				_bodyDef.IsSleepingAllowed = isSleepingAllowed;
			return this;
		}


		public FSRigidBody SetIsAwake(bool isAwake)
		{
			if (Body != null)
				Body.IsAwake = isAwake;
			else
				_bodyDef.IsAwake = isAwake;
			return this;
		}


		public FSRigidBody SetFixedRotation(bool fixedRotation)
		{
			if (Body != null)
				Body.FixedRotation = fixedRotation;
			else
				_bodyDef.FixedRotation = fixedRotation;
			return this;
		}


		public FSRigidBody SetIgnoreGravity(bool ignoreGravity)
		{
			if (Body != null)
				Body.IgnoreGravity = ignoreGravity;
			else
				_bodyDef.IgnoreGravity = ignoreGravity;
			return this;
		}


		public FSRigidBody SetGravityScale(float gravityScale)
		{
			if (Body != null)
				Body.GravityScale = gravityScale;
			else
				_bodyDef.GravityScale = gravityScale;
			return this;
		}


		public FSRigidBody SetMass(float mass)
		{
			if (Body != null)
				Body.Mass = mass;
			else
				_bodyDef.Mass = mass;
			return this;
		}


		public FSRigidBody SetInertia(float inertia)
		{
			if (Body != null)
				Body.Inertia = inertia;
			else
				_bodyDef.Inertia = inertia;
			return this;
		}

		#endregion


		#region Component lifecycle

		public override void Initialize()
		{
		}


		public override void OnAddedToEntity()
		{
			CreateBody();
		}


		public override void OnRemovedFromEntity()
		{
			DestroyBody();
		}


		public override void OnEnabled()
		{
			if (Body != null)
				Body.Enabled = true;
		}


		public override void OnDisabled()
		{
			if (Body != null)
				Body.Enabled = false;
		}


		public override void OnEntityTransformChanged(Transform.Component comp)
		{
			if (_ignoreTransformChanges || Body == null)
				return;

			if (comp == Transform.Component.Position)
				Body.Position = Transform.Position * FSConvert.DisplayToSim;
			else if (comp == Transform.Component.Rotation)
				Body.Rotation = Transform.Rotation;
		}

		#endregion


		public void UpdateTransforms()
		{
			if (Body == null || !Body.IsAwake)
				return;

			_ignoreTransformChanges = true;
			Transform.Position = FSConvert.SimToDisplay * Body.Position;
			Transform.Rotation = Body.Rotation;
			_ignoreTransformChanges = false;
		}


		void CreateBody()
		{
			if (Body != null)
				return;

			var world = Entity.Scene.GetOrCreateSceneComponent<FSWorld>();
			Body = new Body(world, Transform.Position * FSConvert.DisplayToSim, Transform.Rotation, _bodyDef.BodyType,
				this);
			Body.LinearVelocity = _bodyDef.LinearVelocity;
			Body.AngularVelocity = _bodyDef.AngularVelocity;
			Body.LinearDamping = _bodyDef.LinearDamping;
			Body.AngularDamping = _bodyDef.AngularDamping;

			Body.IsBullet = _bodyDef.IsBullet;
			Body.IsSleepingAllowed = _bodyDef.IsSleepingAllowed;
			Body.IsAwake = _bodyDef.IsAwake;
			Body.Enabled = Enabled;
			Body.FixedRotation = _bodyDef.FixedRotation;
			Body.IgnoreGravity = _bodyDef.IgnoreGravity;
			Body.Mass = _bodyDef.Mass;
			Body.Inertia = _bodyDef.Inertia;

			var collisionShapes = Entity.GetComponents<FSCollisionShape>();
			for (var i = 0; i < collisionShapes.Count; i++)
				collisionShapes[i].CreateFixture();
			ListPool<FSCollisionShape>.Free(collisionShapes);

			for (var i = 0; i < _joints.Count; i++)
				_joints[i].CreateJoint();
			
			SetCallbacks();
		}

		public void SetCallbacks() {
			Body.OnCollision += OnCollisionEvent;
			Body.OnSeparation += OnSeparationEvent;
		}
		public void UnsetCallbacks() {
			Body.OnCollision -= OnCollisionEvent;
			Body.OnSeparation -= OnSeparationEvent;
		}

		void DestroyBody()
		{
			for (var i = 0; i < _joints.Count; i++)
				_joints[i].DestroyJoint();
			_joints.Clear();

			var collisionShapes = Entity.GetComponents<FSCollisionShape>();
			for (var i = 0; i < collisionShapes.Count; i++)
				collisionShapes[i].DestroyFixture();
			ListPool<FSCollisionShape>.Free(collisionShapes);
			
			UnsetCallbacks();

			Body.World.RemoveBody(Body);
			Body = null;
		}

		public void IgnoreCollisionWith(FSRigidBody other) {
			Body.IgnoreCollisionWith(other.Body);
		}
		public void RestoreCollisionWith(FSRigidBody other) {
			Body.RestoreCollisionWith(other.Body);
		}

		bool OnCollisionEvent(Fixture fixtureA, Fixture fixtureB, Contact contact) {
			var shape = (FSCollisionShape)fixtureA.UserData;
			var otherShape = (FSCollisionShape)fixtureB.UserData;
			collisionEvents.Add(new CollisionEventInfo {
				other = otherShape,
				contact = contact
			});
			// TODO: concern here because contact data can come from other body
			// var otherBody = otherShape.GetComponent<FSRigidBody>();
			// otherBody.collisionEvents.Add(new CollisionEventInfo {
			// 	other = shape,
			// 	contact = contact
			// });
			
			return true;
		}
		void OnSeparationEvent(Fixture fixtureA, Fixture fixtureB) {
			var shape = (FSCollisionShape)fixtureA.UserData;
			var otherShape = (FSCollisionShape)fixtureB.UserData;
			separationEvents.Add(new SeparationEventInfo {
				other = otherShape,
			});
			var otherBody = otherShape.GetComponent<FSRigidBody>();
			otherBody.separationEvents.Add(new SeparationEventInfo {
				other = shape
			});
		}


		public void Reset() {
			collisionEvents.Clear();
			separationEvents.Clear();
		}


		IEnumerable<ContactInfo> EnumerateContactList() {
			var first = Body.ContactList;
			var current = first;
			while(current != null) {
				var res = new ContactInfo {
					other = (FSRigidBody)current.Other.UserData,
					contact = current.Contact
				};
				current = current.Next;
				yield return res;
			}
		}


		public struct CollisionEventInfo {
			public FSCollisionShape other;
			public Contact contact; 
		}
		public struct ContactInfo {
			public FSRigidBody other;
			public Contact contact; 
		}
		public struct SeparationEventInfo {
			public FSCollisionShape other;
		}
	}
}