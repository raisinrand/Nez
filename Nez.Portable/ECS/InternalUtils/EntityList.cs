using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace Nez
{
	public class EntityList
	{
		public Scene Scene;

		/// <summary>
		/// list of entities added to the scene
		/// </summary>
		FastList<Entity> _entities = new FastList<Entity>();

		/// <summary>
		/// The list of entities that were added this frame. Used to group the entities so we can process them simultaneously
		/// </summary>
		// HashSet<Entity> _entitiesToAdd = new HashSet<Entity>();

		/// <summary>
		/// The list of entities that were marked for removal this frame. Used to group the entities so we can process them simultaneously
		/// </summary>
		// HashSet<Entity> _entitiesToRemove = new HashSet<Entity>();

		/// <summary>
		/// flag used to determine if we need to sort our entities this frame
		/// </summary>
		bool _isEntityListUnsorted;

		/// <summary>
		/// tracks entities by tag for easy retrieval
		/// </summary>
		Dictionary<int, FastList<Entity>> _entityDict = new Dictionary<int, FastList<Entity>>();

		HashSet<int> _unsortedTags = new HashSet<int>();

		// used in updateLists to double buffer so that the original lists can be modified elsewhere
		HashSet<Entity> _tempEntityList = new HashSet<Entity>();


		public EntityList(Scene scene)
		{
			Scene = scene;
		}

		#region array access

		public int Count => _entities.Length;

		public Entity this[int index] => _entities.Buffer[index];

		#endregion


		public void MarkEntityListUnsorted()
		{
			_isEntityListUnsorted = true;
		}

		internal void MarkTagUnsorted(int tag)
		{
			_unsortedTags.Add(tag);
		}

		/// <summary>
		/// adds an Entity to the list. All lifecycle methods will be called in the next frame.
		/// </summary>
		/// <param name="entity">Entity.</param>
		public void Add(Entity entity)
		{
			_entities.Add(entity);
			entity.Scene = Scene;

			// handle the tagList
			AddToTagList(entity);

			Scene.EntityProcessors.OnEntityAdded(entity);
			entity.OnAddedToScene();
			_isEntityListUnsorted = true;
		}

		/// <summary>
		/// removes an Entity from the list. All lifecycle methods will be called in the next frame.
		/// </summary>
		/// <param name="entity">Entity.</param>
		public void Remove(Entity entity)
		{
			// handle the tagList
			RemoveFromTagList(entity);

			// handle the regular entity list
			_entities.Remove(entity);
			entity.OnRemovedFromScene();
			entity.Scene = null;

			Scene.EntityProcessors.OnEntityRemoved(entity);
			_isEntityListUnsorted = true;
		}

		/// <summary>
		/// removes all entities from the entities list
		/// </summary>
		public void RemoveAllEntities()
		{
			// why do we update lists here? Mainly to deal with Entities that were detached before a Scene switch. They will still
			// be in the _entitiesToRemove list which will get handled by updateLists.
			UpdateLists();

			for (var i = 0; i < _entities.Length; i++)
			{
				_entities.Buffer[i]._isDestroyed = true;
				_entities.Buffer[i].OnRemovedFromScene();
				_entities.Buffer[i].Scene = null;
			}

			_entities.Clear();
			_entityDict.Clear();
		}

		/// <summary>
		/// checks to see if the Entity is presently managed by this EntityList
		/// </summary>
		/// <param name="entity">Entity.</param>
		public bool Contains(Entity entity)
		{
			return _entities.Contains(entity);
		}

		FastList<Entity> GetTagList(int tag)
		{
			FastList<Entity> list = null;
			if (!_entityDict.TryGetValue(tag, out list))
			{
				list = new FastList<Entity>();
				_entityDict[tag] = list;
			}

			return _entityDict[tag];
		}

		internal void AddToTagList(Entity entity)
		{
			var list = GetTagList(entity.Tag);
			if (!list.Contains(entity))
			{
				list.Add(entity);
				_unsortedTags.Add(entity.Tag);
			}
		}

		internal void RemoveFromTagList(Entity entity)
		{
			FastList<Entity> list = null;
			if (_entityDict.TryGetValue(entity.Tag, out list))
			{
				list.Remove(entity);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Update()
		{
			for (var i = 0; i < _entities.Length; i++)
			{
				var entity = _entities.Buffer[i];
				if (entity.Enabled && (entity.UpdateInterval == 1 || Time.FrameCount % entity.UpdateInterval == 0))
					entity.Update();
			}
		}

		public void UpdateLists()
		{
			if (_isEntityListUnsorted)
			{
				_entities.Sort();
				_isEntityListUnsorted = false;
			}

			// sort our tagList if needed
			if (_unsortedTags.Count > 0)
			{
				foreach (var tag in _unsortedTags)
					_entityDict[tag].Sort();
				_unsortedTags.Clear();
			}
		}


		#region Entity search

		/// <summary>
		/// returns the first Entity found with a name of name. If none are found returns null.
		/// </summary>
		/// <returns>The entity.</returns>
		/// <param name="name">Name.</param>
		public Entity FindEntity(string name)
		{
			for (var i = 0; i < _entities.Length; i++)
			{
				if (_entities.Buffer[i].Name == name)
					return _entities.Buffer[i];
			}

			return null;
		}

		/// <summary>
		/// returns a list of all entities with tag. If no entities have the tag an empty list is returned. The returned List can be put back in the pool via ListPool.free.
		/// </summary>
		/// <returns>The with tag.</returns>
		/// <param name="tag">Tag.</param>
		public List<Entity> EntitiesWithTag(int tag)
		{
			var list = GetTagList(tag);

			var returnList = ListPool<Entity>.Obtain();
			returnList.Capacity = _entities.Length;
			for (var i = 0; i < list.Length; i++)
			{
				returnList.Add(list[i]);
			}

			return returnList;
		}

		/// <summary>
		/// returns a List of all Entities of type T. The returned List can be put back in the pool via ListPool.free.
		/// </summary>
		/// <returns>The of type.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public List<T> EntitiesOfType<T>() where T : Entity
		{
			var list = ListPool<T>.Obtain();
			for (var i = 0; i < _entities.Length; i++)
			{
				if (_entities.Buffer[i] is T)
					list.Add((T)_entities.Buffer[i]);
			}

			return list;
		}

		/// <summary>
		/// returns the first Component found in the Scene of type T
		/// </summary>
		/// <returns>The component of type.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T FindComponentOfType<T>() where T : Component
		{
			for (var i = 0; i < _entities.Length; i++)
			{
				if (_entities.Buffer[i].Enabled)
				{
					var comp = _entities.Buffer[i].GetComponent<T>();
					if (comp != null)
						return comp;
				}
			}

			return null;
		}

		/// <summary>
		/// returns all Components found in the Scene of type T. The returned List can be put back in the pool via ListPool.free.
		/// </summary>
		/// <returns>The components of type.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public List<T> FindComponentsOfType<T>() where T : Component
		{
			var comps = ListPool<T>.Obtain();
			for (var i = 0; i < _entities.Length; i++)
			{
				if (_entities.Buffer[i].Enabled)
					_entities.Buffer[i].GetComponents<T>(comps);
			}

			return comps;
		}

		#endregion
	}
}