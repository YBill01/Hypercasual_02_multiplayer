using BayatGames.SaveGameFree;
using Cysharp.Threading.Tasks;
using System.IO;
using Unity.Services.CloudSave;
using Unity.Services.Core;

namespace GameName.PlayerProfile
{
	public class ProfileController<T> : IProfileController where T : class, IProfileData, new()
	{
		public int indexData = 0;
		public string identifier = "_save.dat";

		public T Data { get; private set; }

		public ProfileController(string identifier)
		{
			this.identifier = identifier;
		}

		public T Save()
		{
			SaveGame.Save(GetIdentifier(), Data);

			return Data;
		}
		public T Load()
		{
			Data = SaveGame.Load(GetIdentifier(), Clear());

			return Data;
		}

		public async UniTask<T> SaveCloudAsync()
		{
			Stream stream = new MemoryStream();
			SaveGame.Serializer.Serialize(Data, stream, SaveGame.DefaultEncoding);

			try
			{
				await CloudSaveService.Instance.Files.Player.SaveAsync(GetIdentifier(), stream);
			}
			catch (System.Exception)
			{
				return Data;
			}

			return Data;
		}
		public async UniTask<T> LoadCloudAsync()
		{
			try
			{
				Stream stream = await CloudSaveService.Instance.Files.Player.LoadStreamAsync(GetIdentifier());

				Data = SaveGame.Serializer.Deserialize<T>(stream, SaveGame.DefaultEncoding);
			}
			catch (System.Exception)
			{
				return Clear();
			}

			return Data;
		}

		public T Clear()
		{
			Data ??= new T();
			Data.SetDefault();

			return Data;
		}

		public string GetIdentifier()
		{
			return $"{Path.GetFileNameWithoutExtension(identifier)}{indexData:D4}{Path.GetExtension(identifier)}";
		}
	}
}