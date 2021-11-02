namespace Jroynoel.Conveyor
{
	[System.Serializable]
	public abstract class ConveyorStep
	{
		public Conveyor Conveyor { get; set; }

		public virtual void OnAwake() { }
		public virtual void OnStart() { }
		public virtual void OnUpdate() { }
	}
}