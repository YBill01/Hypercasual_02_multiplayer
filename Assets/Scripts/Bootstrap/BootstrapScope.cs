using GameName.PlayerProfile;
using GameName.Data;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class BootstrapScope : LifetimeScope
{
	[Space]
	[SerializeField]
	private App m_app;
	
	[SerializeField]
	private ScenesData m_scenes;

	[Space]
	[SerializeField]
	private UIMessageComponent m_messagePrefab;

	protected override void Awake()
	{
		base.Awake();

		DontDestroyOnLoad(this);
	}

	protected override void Configure(IContainerBuilder builder)
	{
		builder.Register<LoaderService>(Lifetime.Singleton);
		
		builder.RegisterComponent(m_app);

		builder.RegisterInstance(m_scenes);

		builder.Register<Profile>(Lifetime.Singleton);

		builder.RegisterInstance(m_messagePrefab);

		builder.RegisterEntryPoint<BootstrapFlow>();
	}
}