using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

public class SaveGameController : MonoBehaviour {

	[System.Serializable]
	public class PreviousSong
	{
		public string name;
		public string genre;
		public string artist;
		public int chartPosition;
		public int finalQuality;
		public int sales;
		public int turnOfRelease;
		public PreviousSong(string songTitle, string songGenre, string songArtist, int pos, int quality, int songSales, int turnOfRelease)
		{
			name = songTitle;
			genre = songGenre;
			artist = songArtist;
			finalQuality = quality;
			chartPosition = pos;
			sales = songSales;
			this.turnOfRelease = turnOfRelease;
		}
	}

	public string dateCreated = "";

	[System.Serializable]
	public class MusicStudioSave
	{
		public string saveGameVersion;
		public string gameCreatedDate;
		public string lastSaveDate;
		public string userEmail;
		public bool isNewGame;
		public bool gameCompleted;
		public int initialSeed;
		public string seedState;
		public int currentTurn;
		public float cash;
		public float[] followers;
		public List<Band.SaveData> signedBands; // Bands currently signed
		public List<Band.SaveData> availableBands; // Bands currently signed
		public List<Song.LoggedData> songsInRecording; // Songs currently in recording by a band that exists
		public List<Song.LoggedData> songsBeenReleased; // To recreate songs listen numbers after release, but we may have fired the artist who created it
		public List<PreviousSong> releasedSongs; // List of songs released for top charts data
		public List<MarketingInsightObject> currentInsights; // Insights player has on songs in recording now
		public List<MarketingInsightObject> pastInsights; // Insights player has made in the past
		public List<MarketingInsightObject.LoggedData> currentInsightsLogs; // Because some variables don't get serialized for some reason
		public List<MarketingInsightObject.LoggedData> pastInsightsLogs; // Because some variables don't get serialized for some reason
		public List<Surge.SavedData> activeSurgeList;
		public int[] unlockLevels;
		public bool[] completedTutorials;
		public int[] sampleLevels;
		public int[] sampleIteration;
		public List<int> isGenreSampledAt;
		public List<int> isMoodSampledAt;
		public List<int> isTopicSampledAt;
		public int storageSlots;
		public float previousSales;
		public float previousListens;
		public float previousSalaries;
		public float previousDataStorage;
		public float[] floatValues;
		// we must keep 31 weeks x 18 traits x 6 locations = 3348
		//public float[,,] graphValues;
		public float[] graphValues;

		public MusicStudioSave()
		{
			signedBands = new List<Band.SaveData>();
			availableBands = new List<Band.SaveData>();
			songsInRecording = new List<Song.LoggedData>();
			songsBeenReleased = new List<Song.LoggedData>();
			releasedSongs = new List<PreviousSong>();
			pastInsights = new List<MarketingInsightObject>();
			currentInsights = new List<MarketingInsightObject>();
			currentInsightsLogs = new List<MarketingInsightObject.LoggedData>();
			pastInsightsLogs = new List<MarketingInsightObject.LoggedData>();
			activeSurgeList = new List<Surge.SavedData>();
			unlockLevels = new int[36];
			completedTutorials = new bool[22];
			sampleLevels = new int[3];
			sampleIteration = new int[3];
			followers = new float[6];
			floatValues = new float[108];
			// Loc, Trait, Week
			//graphValues = new float[6, 18, 31];
			graphValues = new float[3348];
		}

		public void ClearLists()
		{
			signedBands.Clear();
			availableBands.Clear();
			songsInRecording.Clear();
			songsBeenReleased.Clear();
			releasedSongs.Clear();
			currentInsights.Clear();
			currentInsightsLogs.Clear();
			pastInsightsLogs.Clear();
			pastInsights.Clear();
		}
	}

	public CurrentCashTooltipControl cashTooltipValues;

	public void SaveGameState(bool endGame)
	{
		MusicStudioSave saveData = new MusicStudioSave();
		CurrGameToSaveData(saveData, endGame);

		// DEBUG UNCOMMENT FOR LOCAL SAVE
		//File.WriteAllText("C:\\Users\\Rob\\Desktop\\TestJson.txt", JsonUtility.ToJson(saveData));
		
		// Write data to firebase
		Persister.SaveCMSData(JsonUtility.ToJson(saveData));

	}
	public void LoadGameState()
	{
		// DEBUG UNCOMMENT FOR LOCAL SAVE
		/*
		MusicStudioSave loadedData = new MusicStudioSave();
		loadedData = JsonUtility.FromJson<MusicStudioSave>(File.ReadAllText("C:\\Users\\Rob\\Desktop\\TestJson.txt"));
		SaveDataToCurrGame(loadedData);
		*/
		GameRefs.I.m_gameController.MainCanvasRaycaster.enabled = false;
		StartCoroutine(LoadGameAsync());
	}

	public void LoadCallback(string status)
	{
		MusicStudioSave loadedData = new MusicStudioSave();
		loadedData = JsonUtility.FromJson<MusicStudioSave>(status);
		SaveDataToCurrGame(loadedData);
		GameRefs.I.m_gameController.UpdateLoadedStuffToHud();
	}

	public IEnumerator LoadGameAsync()
	{
		string jsonStr = Persister.LoadCMSData();
		yield return null;
	}

	public void CurrGameToSaveData(MusicStudioSave saveData, bool isEndGame)
	{
		saveData.saveGameVersion = "V1.1";
		Debug.Log(System.DateTime.Now.ToString());
		if (dateCreated == null || dateCreated == "")
		{
			dateCreated = System.DateTime.Now.ToString();
			saveData.gameCreatedDate = dateCreated;
		}
		else
		{
			saveData.gameCreatedDate = dateCreated;
		}
		saveData.lastSaveDate = System.DateTime.Now.ToString();
		saveData.userEmail = GameRefs.I.m_restLogger.GetUserEmail() == null ? "" : GameRefs.I.m_restLogger.GetUserEmail();

		saveData.seedState = JsonUtility.ToJson(Random.state);

		saveData.ClearLists();
		saveData.gameCompleted = isEndGame;

		saveData.currentTurn = GameRefs.I.m_gameController.currentTurn;
		saveData.cash = GameRefs.I.m_gameController.GetCash();

		for(int i = 0; i < 6; i++)
			saveData.followers[i] = GameRefs.I.m_gameController.GetFollowersAtLocation(StatSubType.List[StatSubType.BOOKLINE_ID + i]);

		for (int i = 0; i < GameRefs.I.m_gameController.GetSignedBands().Count; i++)
			saveData.signedBands.Add(GameRefs.I.m_gameController.GetSignedBands()[i].GetSavedData());

		for (int i = 0; i < GameRefs.I.m_gameController.GetAvailableBands().Count; i++)
			saveData.availableBands.Add(GameRefs.I.m_gameController.GetAvailableBands()[i].GetSavedData());

		for (int i = 0; i < GameRefs.I.m_gameController.GetRecordingSongs().Count; i++)
			saveData.songsInRecording.Add(GameRefs.I.m_gameController.GetRecordingSongs()[i].GetDataForLogs());

		for(int i = 0; i < GameRefs.I.m_topCharts.songSalesList.Count; i++)
			saveData.releasedSongs.Add(GameRefs.I.m_topCharts.songSalesList[i]);

		for (int i = 0; i < GameRefs.I.m_topCharts.songObjectSalesList.Count; i++)
			saveData.songsBeenReleased.Add(GameRefs.I.m_topCharts.songObjectSalesList[i].GetDataForLogs());

		for (int i = 0; i < GameRefs.I.m_dataSimulationManager.activeSurges.Count; i++)
			saveData.activeSurgeList.Add(GameRefs.I.m_dataSimulationManager.activeSurges[i].GetSavedData());

		saveData.currentInsights = GameRefs.I.m_gameController.marketingInsightList;
		for(int i = 0; i < saveData.currentInsights.Count; i++)
			saveData.currentInsightsLogs.Add(saveData.currentInsights[i].GetDataForLogs());

		saveData.pastInsights = GameRefs.I.m_gameController.previousMarketingInsights;
		for (int i = 0; i < saveData.pastInsights.Count; i++)
			saveData.pastInsightsLogs.Add(saveData.pastInsights[i].GetDataForLogs());

		int unlockIndex = 0;
		foreach(KeyValuePair<StatSubType, MarketingView.CardUnlocks[]> key in GameRefs.I.m_marketingView.GetBoroughUnlocks())
		{
			for(int i = 0; i < 3; i++)
			{
				saveData.unlockLevels[unlockIndex] = key.Value[i].getNumUnlocks();
				unlockIndex++;
			}
		}
		foreach (KeyValuePair<StatSubType, MarketingView.CardUnlocks[]> key in GameRefs.I.m_marketingView.GetGenreUnlocks())
		{
			for (int i = 0; i < 3; i++)
			{
				saveData.unlockLevels[unlockIndex] = key.Value[i].getNumUnlocks();
				unlockIndex++;
			}
		}

		for(int i = 0; i < GameRefs.I.m_tutorialController.events.Length; i++)
		{
			saveData.completedTutorials[i] = GameRefs.I.m_tutorialController.events[i].completed;
		}

		saveData.sampleLevels[0] = GameRefs.I.traitSamplings[StatType.GENRE].slotCount;
		saveData.sampleIteration[0] = GameRefs.I.traitSamplings[StatType.GENRE].iteration;

		saveData.sampleLevels[1] = GameRefs.I.traitSamplings[StatType.MOOD].slotCount;
		saveData.sampleIteration[1] = GameRefs.I.traitSamplings[StatType.MOOD].iteration;

		saveData.sampleLevels[2] = GameRefs.I.traitSamplings[StatType.TOPIC].slotCount;
		saveData.sampleIteration[2] = GameRefs.I.traitSamplings[StatType.TOPIC].iteration;
		saveData.storageSlots = GameRefs.I.storageSlotCount;

		// Persist isSampledAt.
		saveData.isGenreSampledAt = GameRefs.I.isSampledAt[StatType.GENRE].ToIntList();
		saveData.isMoodSampledAt = GameRefs.I.isSampledAt[StatType.MOOD].ToIntList();
		saveData.isTopicSampledAt = GameRefs.I.isSampledAt[StatType.TOPIC].ToIntList();

		saveData.previousDataStorage = cashTooltipValues.saveStorageCosts;
		saveData.previousListens = cashTooltipValues.saveCashFromListens;
		saveData.previousSalaries = cashTooltipValues.saveBandUpkeep;
		saveData.previousSales = cashTooltipValues.saveCashFromSongs;

		saveData.floatValues = GameRefs.I.m_dataSimulationManager.SavePersonFloatVals();

		Dictionary<StatSubType, List<float>> data;

		for (int loc = 0; loc < 6; loc++)
		{
			data = GameRefs.I.m_dataSimulationManager.GetCachedIndustryLocationData(StatSubType.List[StatSubType.BOOKLINE_ID + loc]);

			for(int mood = 0; mood < 6; mood++)
			{
				for(int week = 0; week < 31; week++)
				{
					if(week < data[StatSubType.List[StatSubType.MOOD1_ID + mood]].Count)
						saveData.graphValues[loc * 558 + mood * 31 + week] = data[StatSubType.List[StatSubType.MOOD1_ID + mood]][week];
					else
						saveData.graphValues[loc * 558 + mood * 31 + week] = 0f;
				}
			}
			for (int topic = 0; topic < 6; topic++)
			{
				for (int week = 0; week < 31; week++)
				{
					if (week < data[StatSubType.List[StatSubType.TOPIC1_ID + topic]].Count)
						saveData.graphValues[loc * 558 + (topic + 6) * 31 + week] = data[StatSubType.List[StatSubType.TOPIC1_ID + topic]][week];
					else
						saveData.graphValues[loc * 558 + (topic + 6) * 31 + week] = 0f;
				}
			}
			for (int genre = 0; genre < 6; genre++)
			{
				for (int week = 0; week < 31; week++)
				{
					if (week < data[StatSubType.List[StatSubType.ROCK_ID + genre]].Count)
						saveData.graphValues[loc * 558 + (genre + 12) * 31 + week] = data[StatSubType.List[StatSubType.ROCK_ID + genre]][week];
					else
						saveData.graphValues[loc * 558 + (genre + 12) * 31 + week] = 0f;
				}
			}

		}
	}

	public void SaveDataToCurrGame(MusicStudioSave saveData)
	{
		dateCreated = saveData.gameCreatedDate;

		GameRefs.I.m_gameController.currentTurn = saveData.currentTurn;
		GameRefs.I.m_gameController.SetCash(saveData.cash);

		GameRefs.I.m_gameController.ClearAndSetBands(saveData.signedBands, saveData.availableBands);
		GameRefs.I.m_dataSimulationManager.LoadSurges(saveData.activeSurgeList);

		cashTooltipValues.saveStorageCosts = saveData.previousDataStorage;
		cashTooltipValues.saveCashFromListens = saveData.previousListens;
		cashTooltipValues.saveBandUpkeep = saveData.previousSalaries;
		cashTooltipValues.saveCashFromSongs = saveData.previousSales;

		GameRefs.I.traitSamplings[StatType.GENRE].slotCount = saveData.sampleLevels[0];
		GameRefs.I.traitSamplings[StatType.GENRE].iteration = saveData.sampleIteration[0];

		GameRefs.I.traitSamplings[StatType.MOOD].slotCount = saveData.sampleLevels[1];
		GameRefs.I.traitSamplings[StatType.MOOD].iteration = saveData.sampleIteration[1];

		GameRefs.I.traitSamplings[StatType.TOPIC].slotCount = saveData.sampleLevels[2];
		GameRefs.I.traitSamplings[StatType.TOPIC].iteration = saveData.sampleIteration[2];
		GameRefs.I.storageSlotCount = saveData.storageSlots;

		GameRefs.I.isSampledAt[StatType.GENRE] = saveData.isGenreSampledAt.ToNullableBoolList();
		GameRefs.I.isSampledAt[StatType.MOOD] = saveData.isMoodSampledAt.ToNullableBoolList();
		GameRefs.I.isSampledAt[StatType.TOPIC] = saveData.isTopicSampledAt.ToNullableBoolList();

		for (int i = 0; i < GameRefs.I.m_tutorialController.events.Length; i++)
		{
			if(saveData.completedTutorials[i])
			{
				GameRefs.I.m_tutorialController.SetTutorialCompleted(GameRefs.I.m_tutorialController.events[i].id, true);
			}
		}

		int unlockIndex = 0;
		foreach (KeyValuePair<StatSubType, MarketingView.CardUnlocks[]> key in GameRefs.I.m_marketingView.GetBoroughUnlocks())
		{
			for (int i = 0; i < 3; i++)
			{
				key.Value[i].setNumUnlocks(saveData.unlockLevels[unlockIndex]);
				unlockIndex++;
			}
		}
		foreach (KeyValuePair<StatSubType, MarketingView.CardUnlocks[]> key in GameRefs.I.m_marketingView.GetGenreUnlocks())
		{
			for (int i = 0; i < 3; i++)
			{
				key.Value[i].setNumUnlocks(saveData.unlockLevels[unlockIndex]);
				unlockIndex++;
			}
		}

		GameRefs.I.m_gameController.marketingInsightList.Clear();
		for(int i = 0; i < saveData.currentInsights.Count; i++)
		{
			foreach(Band b in GameRefs.I.m_gameController.GetSignedBands())
			{
				if(b.Name == saveData.currentInsights[i].bandAttached.Name)
				{
					// Band equality isn't exactly the same when loading, so just check name and overwrite
					saveData.currentInsights[i].bandAttached = b;
					break;
				}
			}
			saveData.currentInsights[i].location = StatSubType.GetTypeFromString(saveData.currentInsightsLogs[i].location);
			saveData.currentInsights[i].statType = StatSubType.GetTypeFromString(saveData.currentInsightsLogs[i].statType);
			GameRefs.I.m_gameController.marketingInsightList.Add(saveData.currentInsights[i]);
		}
			

		GameRefs.I.m_gameController.previousMarketingInsights.Clear();
		for(int i = 0; i < saveData.pastInsights.Count; i++)
		{
			saveData.pastInsights[i].location = StatSubType.GetTypeFromString(saveData.pastInsightsLogs[i].location);
			saveData.pastInsights[i].statType = StatSubType.GetTypeFromString(saveData.pastInsightsLogs[i].statType);
			GameRefs.I.m_gameController.previousMarketingInsights.Add(saveData.pastInsights[i]);
		}
		

		GameRefs.I.m_topCharts.songSalesList.Clear();
		GameRefs.I.m_topCharts.songObjectSalesList.Clear();

		for(int i = 0; i < saveData.releasedSongs.Count; i++)
		{
			GameRefs.I.m_topCharts.songSalesList.Add(saveData.releasedSongs[i]);			
		}
		
		GameRefs.I.m_gameController.AddReleasedSongsToGame(saveData.songsBeenReleased);

		// Set songs, turns, artist, band, etc
		GameRefs.I.m_gameController.AddLoadedSongsToGame(saveData.songsInRecording);

		for (int i = 0; i < 6; i++)
			GameRefs.I.m_gameController.SetFollowersAtLocation(StatSubType.List[StatSubType.BOOKLINE_ID + i], saveData.followers[i]);

		GameRefs.I.m_dataSimulationManager.LoadPersonFloatVals(saveData.floatValues);

		Dictionary<StatSubType, List<float>> data;

		for (int loc = 0; loc < 6; loc++)
		{
			data = GameRefs.I.m_dataSimulationManager.GetCachedIndustryLocationData(StatSubType.List[StatSubType.BOOKLINE_ID + loc]);

			for (int mood = 0; mood < 6; mood++)
			{
				for (int week = 0; week < 31; week++)
				{
					if (week < data[StatSubType.List[StatSubType.MOOD1_ID + mood]].Count)
						data[StatSubType.List[StatSubType.MOOD1_ID + mood]][week] = saveData.graphValues[loc * 558 + mood * 31 + week];
					else
						data[StatSubType.List[StatSubType.MOOD1_ID + mood]][week] = 0f;
				}
			}
			for (int topic = 0; topic < 6; topic++)
			{
				for (int week = 0; week < 31; week++)
				{
					if (week < data[StatSubType.List[StatSubType.TOPIC1_ID + topic]].Count)
						data[StatSubType.List[StatSubType.TOPIC1_ID + topic]][week] = saveData.graphValues[loc * 558 + (topic + 6) * 31 + week];
					else
						data[StatSubType.List[StatSubType.TOPIC1_ID + topic]][week] = 0f;
				}
			}
			for (int genre = 0; genre < 6; genre++)
			{
				for (int week = 0; week < 31; week++)
				{
					if (week < data[StatSubType.List[StatSubType.ROCK_ID + genre]].Count)
						data[StatSubType.List[StatSubType.ROCK_ID + genre]][week] = saveData.graphValues[loc * 558 + (genre + 12) * 31 + week];
					else
						data[StatSubType.List[StatSubType.ROCK_ID + genre]][week] = 0f;
				}
			}

		}

		Random.state = JsonUtility.FromJson<Random.State>(saveData.seedState);
	}
}
