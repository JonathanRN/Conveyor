using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Jroynoel.Conveyor
{
	public class Conveyor : MonoBehaviour
	{
		[HideInInspector]
		[SerializeReference]
		public List<ConveyorStep> Steps;

		protected virtual void Awake()
		{
			foreach (var step in Steps)
			{
				step.Conveyor = this;
				step.OnAwake();
			}
		}

		protected virtual void Start()
		{
			foreach (var step in Steps)
			{
				step.OnStart();
			}
		}

		protected virtual void Update()
		{
			foreach (var step in Steps)
			{
				step.OnUpdate();
			}
		}

		public T GetStep<T>() where T : ConveyorStep => (T)Steps.FirstOrDefault(x => x.GetType() == typeof(T));
	}
}