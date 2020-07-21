﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace Nez
{
	public class ComponentList
	{
		// global updateOrder sort for the IUpdatable list
		static IUpdatableComparer compareUpdatableOrder = new IUpdatableComparer();

		Entity _entity;

		/// <summary>
		/// list of components added to the entity
		/// </summary>
		FastList<Component> _components = new FastList<Component>();

		/// <summary>
		/// list of all Components that want update called
		/// </summary>
		FastList<IUpdatable> _updatableComponents = new FastList<IUpdatable>();

		/// <summary>
		/// The list of components that were added this frame. Used to group the components so we can process them simultaneously
		/// </summary>
		// internal List<Component> _componentsToAdd = new List<Component>();

		/// <summary>
		/// The list of components that were marked for removal this frame. Used to group the components so we can process them simultaneously
		/// </summary>
		// List<Component> _componentsToRemove = new List<Component>();

		List<Component> _tempBufferList = new List<Component>();

		/// <summary>
		/// flag used to determine if we need to sort our Components this frame
		/// </summary>
		bool _isComponentListUnsorted;

		public ComponentList(Entity entity)
		{
			_entity = entity;
		}


		#region array access

		public int Count => _components.Length;

		public Component this[int index] => _components.Buffer[index];

		#endregion


		public void MarkEntityListUnsorted()
		{
			_isComponentListUnsorted = true;
		}

		public void Add(Component component)
		{
			if (component is RenderableComponent)
				_entity.Scene.RenderableComponents.Add(component as RenderableComponent);

			if (component is IUpdatable)
				_updatableComponents.Add(component as IUpdatable);

			_entity.componentBits.Set(ComponentTypeManager.GetIndexFor(component.GetType()));
			_entity.Scene.EntityProcessors.OnComponentAdded(_entity);

			_components.Add(component);
			_tempBufferList.Add(component);
			_isComponentListUnsorted = true;

			component.OnAddedToEntity();

			// component.enabled checks both the Entity and the Component
			if (component.Enabled)
				component.OnEnabled();
		}

		public void Remove(Component component)
		{
			HandleRemove(component);
			_components.Remove(component);
		}

		/// <summary>
		/// removes all components from the component list immediately
		/// </summary>
		public void RemoveAllComponents()
		{
			for (var i = 0; i < _components.Length; i++)
				HandleRemove(_components.Buffer[i]);

			_components.Clear();
			_updatableComponents.Clear();
		}

		internal void DeregisterAllComponents()
		{
			for (var i = 0; i < _components.Length; i++)
			{
				var component = _components.Buffer[i];

				// deal with renderLayer list if necessary
				if (component is RenderableComponent)
					_entity.Scene.RenderableComponents.Remove(component as RenderableComponent);

				// deal with IUpdatable
				if (component is IUpdatable)
					_updatableComponents.Remove(component as IUpdatable);

					_entity.componentBits.Set(ComponentTypeManager.GetIndexFor(component.GetType()), false);
					_entity.Scene.EntityProcessors.OnComponentRemoved(_entity);
			}
		}

		internal void RegisterAllComponents()
		{
			for (var i = 0; i < _components.Length; i++)
			{
				var component = _components.Buffer[i];
				if (component is RenderableComponent)
					_entity.Scene.RenderableComponents.Add(component as RenderableComponent);

				if (component is IUpdatable)
					_updatableComponents.Add(component as IUpdatable);

				_entity.componentBits.Set(ComponentTypeManager.GetIndexFor(component.GetType()));
				_entity.Scene.EntityProcessors.OnComponentAdded(_entity);
			}
		}


		/// <summary>
		/// handles any Components that need to be removed or added
		/// </summary>
		void UpdateLists()
		{
			if (_isComponentListUnsorted)
			{
				_updatableComponents.Sort(compareUpdatableOrder);
				_isComponentListUnsorted = false;
			}
		}

		void HandleRemove(Component component)
		{
			// deal with renderLayer list if necessary
			if (component is RenderableComponent)
				_entity.Scene.RenderableComponents.Remove(component as RenderableComponent);

			// deal with IUpdatable
			if (component is IUpdatable)
				_updatableComponents.Remove(component as IUpdatable);

			_entity.componentBits.Set(ComponentTypeManager.GetIndexFor(component.GetType()), false);
			_entity.Scene.EntityProcessors.OnComponentRemoved(_entity);

			component.OnRemovedFromEntity();
			component.Entity = null;
		}

		/// <summary>
		/// Gets the first component of type T and returns it. Optionally skips checking un-initialized Components (Components who have not yet had their
		/// onAddedToEntity method called). If no components are found returns null.
		/// </summary>
		/// <returns>The component.</returns>
		/// <param name="onlyReturnInitializedComponents">If set to <c>true</c> only return initialized components.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T GetComponent<T>() where T : Component
		{
			for (var i = 0; i < _components.Length; i++)
			{
				var component = _components.Buffer[i];
				if (component is T)
					return component as T;
			}
			return null;
		}

		/// <summary>
		/// Gets the first component of type T and returns it. Optionally skips checking un-initialized Components (Components who have not yet had their
		/// onAddedToEntity method called). If no components are found returns null.
		/// </summary>
		/// <returns>The component.</returns>
		/// <param name="onlyReturnInitializedComponents">If set to <c>true</c> only return initialized components.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Component GetComponent(Type type)
		{
			for (var i = 0; i < _components.Length; i++)
			{
				var component = _components.Buffer[i];
				if (type.IsAssignableFrom(component.GetType()))
					return component;
			}

			return null;
		}

		/// <summary>
		/// Gets all the components of type type without a List allocation
		/// </summary>
		/// <param name="components">Components.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void GetComponents(Type type, List<Component> components)
		{
			for (var i = 0; i < _components.Length; i++)
			{
				var component = _components.Buffer[i];
				if (type.IsAssignableFrom(component.GetType()))
					components.Add(component);
			}
		}


		/// <summary>
		/// Gets all the components of type T without a List allocation
		/// </summary>
		/// <param name="components">Components.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void GetComponents<T>(List<T> components) where T : class
		{
			for (var i = 0; i < _components.Length; i++)
			{
				var component = _components.Buffer[i];
				if (component is T)
					components.Add(component as T);
			}
		}

		/// <summary>
		/// Gets all the components of type T. The returned List can be put back in the pool via ListPool.free.
		/// </summary>
		/// <returns>The components.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public List<T> GetComponents<T>() where T : class
		{
			var components = ListPool<T>.Obtain();
			GetComponents(components);

			return components;
		}


		/// <summary>
		/// Gets the first component of type T and returns it. Optionally skips checking un-initialized Components (Components who have not yet had their
		/// onAddedToEntity method called). If no components are found returns null.
		/// </summary>
		/// <returns>The component.</returns>
		/// <param name="onlyReturnInitializedComponents">If set to <c>true</c> only return initialized components.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ReplaceComponent(Component replacement)
		{
			replacement.Entity = _entity;
			for (var i = 0; i < _components.Length; i++)
			{
				var component = _components.Buffer[i];
				if (replacement.GetType() == component.GetType()) {
					component.Entity = null;
					_components.Buffer[i] = replacement;
					return;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Update()
		{
			UpdateLists();
			for (var i = 0; i < _updatableComponents.Length; i++)
			{
				if (_updatableComponents.Buffer[i].Enabled && (_updatableComponents.Buffer[i] as Component).Enabled)
					_updatableComponents.Buffer[i].Update();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void OnEntityTransformChanged(Transform.Component comp)
		{
			for (var i = 0; i < _components.Length; i++)
			{
				if (_components.Buffer[i].Enabled)
					_components.Buffer[i].OnEntityTransformChanged(comp);
			}
		}

		internal void OnEntityEnabled()
		{
			for (var i = 0; i < _components.Length; i++)
				_components.Buffer[i].OnEnabled();
		}

		internal void OnEntityDisabled()
		{
			for (var i = 0; i < _components.Length; i++)
				_components.Buffer[i].OnDisabled();
		}

		internal void DebugRender(Batcher batcher)
		{
			for (var i = 0; i < _components.Length; i++)
			{
				if (_components.Buffer[i].Enabled)
					_components.Buffer[i].DebugRender(batcher);
			}
		}
	}
}