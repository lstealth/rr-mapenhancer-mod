using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Analytics;
using Audio;
using Avatar;
using Core;
using GalaSoft.MvvmLight.Messaging;
using Game.AccessControl;
using Game.Events;
using Game.Messages;
using Game.Notices;
using Game.Persistence;
using Game.Progression;
using HeathenEngineering.SteamworksIntegration.API;
using Helpers;
using JetBrains.Annotations;
using KeyValue.Runtime;
using Model;
using Model.Database;
using Model.Definition;
using Model.Definition.Data;
using Model.Ops;
using Model.Ops.Timetable;
using Network;
using Network.Client;
using Network.Messages;
using Serilog;
using Track;
using UI.Common;
using UI.Menu;
using UI.SwitchList;
using UI.Tutorial;
using UnityEngine;

namespace Game.State;

public class StateManager : MonoBehaviour
{
	[SerializeField]
	private GlobalGameManager gameManager;

	[SerializeField]
	private AudioLibrary audioLibrary;

	private PropertyObjectManager _propertyObjectManager = new PropertyObjectManager();

	private GameStorage _storage;

	private readonly HashSet<IDisposable> _observers = new HashSet<IDisposable>();

	[NonSerialized]
	private PlayersManager _playersManager;

	private PlayerPropertiesManager _playerPropertiesManager;

	[NonSerialized]
	private ScheduledAudioPlayer _audioPlayer;

	public readonly Ledger Ledger = new Ledger();

	private LoanManager _loanManager;

	private SaveManager _saveManager;

	private TimeObserver _timeObserver;

	private readonly List<(string objectId, string key, Value)> _gameSetupPropertyPresets = new List<(string, string, Value)>();

	private SetupDescriptor _setupDescriptor;

	public static StateManager Shared { get; private set; }

	public static bool IsHost => Multiplayer.IsHost;

	public static bool IsUnloading { get; private set; }

	public static long Now
	{
		get
		{
			if (!(Multiplayer.Client == null))
			{
				return Multiplayer.Client.Tick;
			}
			return NetworkTime.systemTick;
		}
	}

	public static AccessLevel AccessLevel
	{
		get
		{
			if (IsHost)
			{
				return AccessLevel.President;
			}
			ClientManager client = Multiplayer.Client;
			if (client == null)
			{
				Log.Warning("Undetermined AccessLevel: null client");
				return AccessLevel.Undetermined;
			}
			return client.AccessLevel;
		}
	}

	public static bool HasTrainmasterAccess => AccessLevel >= AccessLevel.Trainmaster;

	public GameMode GameMode
	{
		get
		{
			return _storage.GameMode;
		}
		private set
		{
			_storage.GameMode = value;
		}
	}

	public static bool IsSandbox => Shared.GameMode == GameMode.Sandbox;

	public string RailroadName
	{
		get
		{
			return _storage.RailroadName;
		}
		private set
		{
			_storage.RailroadName = value;
		}
	}

	public string RailroadMark
	{
		get
		{
			return _storage.RailroadMark;
		}
		private set
		{
			_storage.RailroadMark = value;
		}
	}

	public int Balance
	{
		get
		{
			return _storage.Balance;
		}
		private set
		{
			_storage.Balance = value;
		}
	}

	public bool IsWaiting { get; private set; }

	private TrainController _trainController => TrainController.Shared;

	private OpsController opsController => OpsController.Shared;

	public GameStorage Storage => _storage;

	public PlayersManager PlayersManager => _playersManager;

	public ScheduledAudioPlayer AudioPlayer => _audioPlayer;

	public LoanManager LoanManager => _loanManager;

	public SaveManager SaveManager => _saveManager;

	[CanBeNull]
	public PlayerRecordsClientManager PlayerRecordsClientManager { get; private set; }

	public bool HasTutorial
	{
		get
		{
			if (!IsHost)
			{
				return false;
			}
			SetupDescriptor setupDescriptor = GetSetupDescriptor();
			if (setupDescriptor != null)
			{
				return setupDescriptor.showTutorial;
			}
			return false;
		}
	}

	public bool HasRestoredProperties => RestoreNotifier.Shared.HasRestored;

	private void Awake()
	{
		_playersManager = base.gameObject.AddComponent<PlayersManager>();
		_timeObserver = base.gameObject.AddComponent<TimeObserver>();
		_audioPlayer = base.gameObject.AddComponent<ScheduledAudioPlayer>();
		_audioPlayer.audioLibrary = audioLibrary;
		_saveManager = GetComponent<SaveManager>();
		CurrencySymbolHelper.SetCurrencySymbol("$");
		if (string.IsNullOrEmpty(Preferences.MultiplayerClientUsername))
		{
			Preferences.MultiplayerClientUsername = User.Client.Id.Name;
		}
	}

	private void OnEnable()
	{
		Shared = this;
		Messenger.Default.Register<MapWillLoadEvent>(this, OnMapWillLoad);
		Messenger.Default.Register<MapDidLoadEvent>(this, OnMapDidLoad);
		Messenger.Default.Register<MapWillUnloadEvent>(this, OnMapWillUnload);
		Messenger.Default.Register<MapDidUnloadEvent>(this, OnMapDidUnload);
		Messenger.Default.Register<PropertiesDidRestore>(this, OnPropertiesDidRestore);
		Messenger.Default.Register<AccessLevelDidChange>(this, OnAccessLevelDidChange);
		Messenger.Default.Register<TimeDayDidChange>(this, OnDayDidChange);
	}

	private void OnDisable()
	{
		Shared = null;
		Messenger.Default.Unregister(this);
	}

	private void OnApplicationQuit()
	{
		IsUnloading = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Init()
	{
		Messenger.Default.Unregister(this);
	}

	private void OnMapWillLoad(MapWillLoadEvent evt)
	{
		TimeWeather.Reset();
		RestoreNotifier.Initialize();
		IsUnloading = false;
		PrepareGameKeyValueObject();
		PreparePlayerProperties();
	}

	private void OnMapDidLoad(MapDidLoadEvent mapDidLoadEvent)
	{
		Log.Debug("StateManager: Map loaded with game mode {gameMode}", GameMode.DisplayString());
	}

	public void ApplyGameSetup(GameSetup? gameSetup)
	{
		if (IsHost)
		{
			SaveManager.SetSaveNameForLaterLoading(gameSetup?.SaveName);
			SaveManager.LoadFromSaveIfNeededOrInitialize();
			if (gameSetup.HasValue && gameSetup.Value.NewGameSetup.HasValue)
			{
				NewGameSetup value = gameSetup.Value.NewGameSetup.Value;
				ApplyNewGameSetup(value);
			}
		}
	}

	private void OnPropertiesDidRestore(PropertiesDidRestore evt)
	{
		_timeObserver.StartObservering();
		_observers.Add(_storage.ObserveGameMode(delegate(GameMode mode)
		{
			Messenger.Default.Send(new GameModeDidChange(mode));
		}, initial: false));
		_observers.Add(_storage.ObserveWeatherId(delegate(int value)
		{
			TimeWeather.WeatherId = value;
		}));
		TimeWeather.TimeMultiplier = 1f;
		Config config = TrainController.Shared.config;
		_observers.Add(_storage.ObserveBrakeForce(delegate(float? maybeForce)
		{
			Car.BrakeForceMultiplier = maybeForce ?? config.brakeForceMultiplier;
		}));
		_observers.Add(_storage.ObserveBrakeForceHandbrake(delegate(float? maybeForce)
		{
			Car.BrakeForceMultiplierHandbrake = maybeForce ?? config.brakeForceMultiplierHandbrake;
		}));
		_observers.Add(_storage.ObserveWearFeature(delegate(bool value)
		{
			Car.WearFeature = value;
		}));
		_observers.Add(_storage.ObserveOilFeature(delegate(bool value)
		{
			Car.OilFeature = Car.WearFeature && value;
		}));
		_observers.Add(_storage.ObserveOverhaulMiles(delegate(int value)
		{
			Car.OverhaulMiles = value;
		}));
		_observers.Add(_storage.ObserveWearMultiplier(delegate(float value)
		{
			Car.WearMultiplier = value;
		}));
		_observers.Add(_storage.ObserveOilUseMultiplier(delegate(float value)
		{
			Car.OilUseMultiplier = value;
		}));
		if (GameMode != GameMode.Sandbox)
		{
			_loanManager = base.gameObject.AddComponent<LoanManager>();
			_loanManager.Configure(_storage);
		}
		if (IsHost)
		{
			Ledger.ReconcileIfNeeded(Balance);
		}
		if (HasTutorial)
		{
			TutorialManager.Shared.ShowIfAppropriateForLaunch();
		}
	}

	private void OnAccessLevelDidChange(AccessLevelDidChange evt)
	{
		Log.Information("Access level changed from {old} to {new}", evt.OldAccessLevel, evt.NewAccessLevel);
		if (evt.NewAccessLevel < AccessLevel.Trainmaster && PlayerRecordsClientManager != null)
		{
			PlayerRecordsClientManager = null;
		}
	}

	private void OnMapWillUnload(MapWillUnloadEvent mapWillUnloadEvent)
	{
		IsUnloading = true;
		if (_loanManager != null)
		{
			UnityEngine.Object.Destroy(_loanManager);
			_loanManager = null;
		}
		SaveManager.WillUnloadMap();
		_playersManager.OnWillUnloadMap();
		_trainController.WillUnloadMap();
		_timeObserver.StopObserving();
		foreach (IDisposable observer in _observers)
		{
			observer.Dispose();
		}
		_observers.Clear();
	}

	private void OnMapDidUnload(MapDidUnloadEvent mapDidUnloadEvent)
	{
		DestroyGameKeyValueObject();
		DestroyPlayerProperties();
		_propertyObjectManager.UnregisterAll();
		RestoreNotifier.Deinitialize();
	}

	private void PrepareGameKeyValueObject()
	{
		DestroyGameKeyValueObject();
		KeyValueObject keyValueObject = base.gameObject.AddComponent<KeyValueObject>();
		_storage = new GameStorage(keyValueObject);
	}

	private void DestroyGameKeyValueObject()
	{
		_storage?.Dispose();
		_storage = null;
	}

	private void PreparePlayerProperties()
	{
		DestroyPlayerProperties();
		GameObject gameObject = new GameObject("Player Properties")
		{
			hideFlags = HideFlags.DontSave
		};
		gameObject.transform.SetParent(base.transform);
		gameObject.AddComponent<KeyValueObject>();
		_playerPropertiesManager = gameObject.AddComponent<PlayerPropertiesManager>();
	}

	private void DestroyPlayerProperties()
	{
		if (!(_playerPropertiesManager == null))
		{
			UnityEngine.Object.Destroy(_playerPropertiesManager.gameObject);
			_playerPropertiesManager = null;
		}
	}

	private void ApplyNewGameSetup(NewGameSetup setup)
	{
		GameMode = setup.Mode;
		RailroadName = setup.RailroadName;
		RailroadMark = setup.ReportingMark;
		_storage.SetupId = setup.SetupId;
		_gameSetupPropertyPresets.Add(("_progression", "progression", Value.String(setup.ProgressionId)));
		SetupDescriptor setupDescriptor = GetSetupDescriptor();
		if (setupDescriptor != null && _trainController.Cars.Count == 0 && _storage.Balance == 0)
		{
			CameraSelector.shared.DefaultSpawn = new PositionRotation(WorldTransformer.WorldToGame(setupDescriptor.spawnPoint.transform.position), setupDescriptor.spawnPoint.transform.rotation);
			StartCoroutine(CompanyModeSetup.Setup(_trainController, setupDescriptor));
		}
	}

	[CanBeNull]
	private SetupDescriptor GetSetupDescriptor()
	{
		string setupId = _storage.SetupId;
		if (string.IsNullOrEmpty(setupId) || _setupDescriptor != null)
		{
			return _setupDescriptor;
		}
		_setupDescriptor = UnityEngine.Object.FindObjectsOfType<SetupDescriptor>().FirstOrDefault((SetupDescriptor sd) => sd.identifier == setupId);
		if (_setupDescriptor == null)
		{
			Log.Error("Failed to find setup: {setupId}", setupId);
		}
		return _setupDescriptor;
	}

	public static List<CarDescriptor> DescriptorsForIdentifiers(IEnumerable<string> identifiers)
	{
		IPrefabStore prefabStore = TrainController.Shared.PrefabStore;
		List<CarDescriptor> list = new List<CarDescriptor>();
		Dictionary<string, Value> dictionary = new Dictionary<string, Value> { 
		{
			"owned",
			Value.Bool(value: true)
		} };
		foreach (string identifier in identifiers)
		{
			TypedContainerItem<CarDefinition> typedContainerItem = prefabStore.CarDefinitionInfoForIdentifier(identifier);
			Dictionary<string, Value> properties = dictionary;
			list.Add(new CarDescriptor(typedContainerItem, default(CarIdent), null, null, flipped: false, properties));
			if (typedContainerItem.Definition.TryGetTenderIdentifier(out var tenderIdentifier))
			{
				TypedContainerItem<CarDefinition> definitionInfo = prefabStore.CarDefinitionInfoForIdentifier(tenderIdentifier);
				properties = dictionary;
				list.Add(new CarDescriptor(definitionInfo, default(CarIdent), null, null, flipped: false, properties));
			}
		}
		return list;
	}

	public static void ApplyLocal(IGameMessage gameMessage)
	{
		StateManager shared = Shared;
		if (shared == null)
		{
			Debug.LogWarning($"No StateManager instance to apply {gameMessage.GetType()}");
			return;
		}
		if (!CheckAuthorizedToSendMessage(gameMessage))
		{
			Log.Warning("Ignoring; failed local authorization: {message}", gameMessage);
			return;
		}
		shared.Handle(gameMessage, shared._playersManager.LocalPlayer);
		if (Multiplayer.Client != null)
		{
			Multiplayer.Client.Send(gameMessage);
		}
	}

	public void Handle(IGameMessage gameMessage, PlayerId playerId)
	{
		IPlayer player = _playersManager.PlayerForId(playerId);
		if (player == null)
		{
			throw new ArgumentException($"playerId \"{playerId}\" not found", "player");
		}
		Handle(gameMessage, player);
	}

	private void Handle(IGameMessage gameMessage, IPlayer sender)
	{
		if (sender == null)
		{
			throw new ArgumentException("null sender", "sender");
		}
		if (!(gameMessage is ICharacterMessage characterMessage))
		{
			if (gameMessage is ICarMessage)
			{
				return;
			}
			if (!(gameMessage is RequestSetSwitch setSwitch))
			{
				if (!(gameMessage is RequestSetSwitchUnlocked setSwitchUnlocked))
				{
					if (!(gameMessage is SetSwitch setSwitch2))
					{
						if (!(gameMessage is SetGladhandsConnected setGladhandsConnected))
						{
							if (!(gameMessage is ManualMoveCar manualMoveCar))
							{
								if (!(gameMessage is Rerail rerail))
								{
									if (!(gameMessage is RequestCarSetIdent requestCarSetIdent))
									{
										if (!(gameMessage is CarSetIdent carSetIdent))
										{
											if (!(gameMessage is CarSetBardo carSetBardo))
											{
												if (!(gameMessage is BatchCarPositionUpdate update))
												{
													if (!(gameMessage is BatchCarAirUpdate update2))
													{
														if (!(gameMessage is PlaceTrain place))
														{
															if (!(gameMessage is AddCars message))
															{
																if (!(gameMessage is RemoveCars message2))
																{
																	if (!(gameMessage is CarSetAdd carSetAdd))
																	{
																		if (!(gameMessage is CarSetRemove carSetRemove))
																		{
																			if (!(gameMessage is CarSetChangeCars carSetChangeCars))
																			{
																				if (!(gameMessage is FireEvent fireEvent))
																				{
																					if (!(gameMessage is SwitchListUpdate switchListUpdate))
																					{
																						if (!(gameMessage is SwitchListToggleCarIds switchListToggleCarIds))
																						{
																							if (!(gameMessage is SwitchListSetCarIds switchListSetCarIds))
																							{
																								if (!(gameMessage is RequestOps request))
																								{
																									if (!(gameMessage is SetTimeOfDay setTimeOfDay))
																									{
																										if (!(gameMessage is WaitTime waitTime))
																										{
																											if (!(gameMessage is SetCarTrainCrew setCarTrainCrew))
																											{
																												if (!(gameMessage is RequestPurchaseEquipment request2))
																												{
																													if (!(gameMessage is PropertyChange change))
																													{
																														if (!(gameMessage is RequestCreateTrainCrew requestCreateTrainCrew))
																														{
																															if (!(gameMessage is RequestDeleteTrainCrew requestDeleteTrainCrew))
																															{
																																if (!(gameMessage is RequestEditTrainCrew requestEditTrainCrew))
																																{
																																	if (!(gameMessage is RequestSetTrainCrewTimetableSymbol requestSetTrainCrewTimetableSymbol))
																																	{
																																		if (!(gameMessage is UpdateTrainCrews updateTrainCrews))
																																		{
																																			if (!(gameMessage is TurntableUpdateAngle turntableUpdateAngle))
																																			{
																																				if (!(gameMessage is TurntableUpdateStopIndex turntableUpdateStopIndex))
																																				{
																																					if (!(gameMessage is Transaction transaction))
																																					{
																																						if (!(gameMessage is PlaySoundAtPosition play))
																																						{
																																							if (!(gameMessage is PlaySoundNotification play2))
																																							{
																																								if (!(gameMessage is ProgressionStartPhase progressionStartPhase))
																																								{
																																									if (!(gameMessage is RequestLoanDelta requestLoanDelta))
																																									{
																																										if (!(gameMessage is AutoEngineerCommand command))
																																										{
																																											if (!(gameMessage is AutoEngineerWaypointRerouteRequest autoEngineerWaypointRerouteRequest))
																																											{
																																												if (!(gameMessage is AutoEngineerContextualOrder contextualOrder))
																																												{
																																													if (!(gameMessage is AutoEngineerWaypointRouteRequest request3))
																																													{
																																														if (!(gameMessage is AutoEngineerWaypointRouteResponse response))
																																														{
																																															if (!(gameMessage is AutoEngineerWaypointRouteUpdate change2))
																																															{
																																																if (!(gameMessage is FlareAddUpdate flareAddUpdate))
																																																{
																																																	if (!(gameMessage is FlareRemove flareRemove))
																																																	{
																																																		if (!(gameMessage is SetPassengerDestinations setPassengerDestinations))
																																																		{
																																																			if (!(gameMessage is SetPassengerAutoDestinations setPassengerAutoDestinations))
																																																			{
																																																				if (!(gameMessage is PlayerRecords playerRecords))
																																																				{
																																																					if (!(gameMessage is RequestSetAccessLevel requestSetAccessLevel))
																																																					{
																																																						if (!(gameMessage is RemovePlayerRecord removePlayerRecord))
																																																						{
																																																							if (!(gameMessage is LedgerRequest ledgerRequest))
																																																							{
																																																								if (!(gameMessage is LedgerResponse ledgerResponse))
																																																								{
																																																									if (!(gameMessage is RequestOilCar requestOilCar))
																																																									{
																																																										if (gameMessage is ModifyContract)
																																																										{
																																																											ModifyContract modifyContract = (ModifyContract)(object)gameMessage;
																																																											if (IsHost)
																																																											{
																																																												opsController.AllIndustries.FirstOrDefault((Industry i) => i.identifier == modifyContract.Id).ModifyContract(modifyContract.Tier);
																																																											}
																																																										}
																																																										else if (!(gameMessage is PostNoticeEphemeral post))
																																																										{
																																																											RepairTrack output;
																																																											if (!(gameMessage is SetRepairMultiplier setRepairMultiplier))
																																																											{
																																																												if (gameMessage is SetTimetable setTimetable && IsHost)
																																																												{
																																																													TimetableController.Shared.HandleSetTimetable(setTimetable.Source, sender);
																																																												}
																																																											}
																																																											else if (IsHost && opsController.TryGetIndustryComponent<RepairTrack>(setRepairMultiplier.Id, out output))
																																																											{
																																																												output.HandleSetMultiplier(setRepairMultiplier.Multiplier);
																																																											}
																																																										}
																																																										else
																																																										{
																																																											NoticeManager.Shared.Handle(post);
																																																										}
																																																									}
																																																									else if (IsHost)
																																																									{
																																																										_trainController.HandleRequestOilCar(requestOilCar.CarId, requestOilCar.Amount);
																																																									}
																																																								}
																																																								else if (!IsHost)
																																																								{
																																																									Ledger.Load(ledgerResponse.Entries, ledgerResponse.StartBalance);
																																																									Messenger.Default.Send(default(LedgerRequestResponseReceived));
																																																								}
																																																							}
																																																							else if (IsHost)
																																																							{
																																																								int startBalance;
																																																								int endBalance;
																																																								IReadOnlyList<Ledger.Entry> source = Ledger.EntriesBetween(new GameDateTime(ledgerRequest.Start), new GameDateTime(ledgerRequest.End), out startBalance, out endBalance);
																																																								SendTo(sender, new LedgerResponse(source.Select((Ledger.Entry e) => new SerializableLedgerEntry(e)).ToList(), startBalance, endBalance));
																																																							}
																																																						}
																																																						else if (IsHost)
																																																						{
																																																							Log.Information("RemovePlayerRecord: {sender} upon {target}", sender, removePlayerRecord.RecordKey);
																																																							HostManager.Shared.RemovePlayerRecord(new PlayerId(removePlayerRecord.RecordKey), sender);
																																																						}
																																																					}
																																																					else if (IsHost)
																																																					{
																																																						Log.Information("RequestSetAccessLevel: {sender} upon {target} to {accessLevel}", sender, requestSetAccessLevel.RecordKey, requestSetAccessLevel.AccessLevel);
																																																						HostManager.Shared.SetAccessLevel(new PlayerId(requestSetAccessLevel.RecordKey), requestSetAccessLevel.AccessLevel, sender);
																																																					}
																																																				}
																																																				else
																																																				{
																																																					if (PlayerRecordsClientManager == null)
																																																					{
																																																						PlayerRecordsClientManager playerRecordsClientManager = (PlayerRecordsClientManager = new PlayerRecordsClientManager());
																																																					}
																																																					PlayerRecordsClientManager.PlayerRecords = playerRecords.Records.ToDictionary((KeyValuePair<string, PlayerRecord> kv) => new PlayerId(kv.Key), (KeyValuePair<string, PlayerRecord> kv) => kv.Value);
																																																					Messenger.Default.Send(default(PlayerRecordsDidChange));
																																																				}
																																																			}
																																																			else if (IsHost)
																																																			{
																																																				PassengerExtensions.SetPassengerTimetableAutoDestinations(setPassengerAutoDestinations.CarId, setPassengerAutoDestinations.Enabled);
																																																			}
																																																		}
																																																		else if (IsHost)
																																																		{
																																																			PassengerExtensions.SetPassengerDestinations(setPassengerDestinations.CarId, setPassengerDestinations.Destinations);
																																																		}
																																																	}
																																																	else if (IsHost)
																																																	{
																																																		FlareManager.Shared.RemoveFlare(flareRemove.Id, sender);
																																																	}
																																																}
																																																else if (IsHost)
																																																{
																																																	FlareManager.Shared.AddFlare(Graph.Shared.MakeLocation(flareAddUpdate.Location), sender);
																																																}
																																															}
																																															else
																																															{
																																																_trainController.HandleAutoEngineerWaypointRouteUpdate(change2);
																																															}
																																														}
																																														else
																																														{
																																															_trainController.HandleAutoEngineerWaypointRouteResponse(response, sender);
																																														}
																																													}
																																													else
																																													{
																																														_trainController.HandleAutoEngineerWaypointRouteRequest(request3, sender);
																																													}
																																												}
																																												else
																																												{
																																													_trainController.HandleAutoEngineerContextualOrder(contextualOrder, sender);
																																												}
																																											}
																																											else
																																											{
																																												_trainController.HandleAutoEngineerWaypointRerouteRequest(autoEngineerWaypointRerouteRequest.LocomotiveId, sender);
																																											}
																																										}
																																										else
																																										{
																																											_trainController.HandleAutoEngineerCommand(command, sender);
																																										}
																																									}
																																									else
																																									{
																																										LoanManager.HandleOffsetLoanRequest(requestLoanDelta.Delta, sender);
																																									}
																																								}
																																								else
																																								{
																																									Game.Progression.Progression.Shared.HandlePayToStartPhase(progressionStartPhase.SectionIdentifier, progressionStartPhase.PhaseIndex, sender);
																																								}
																																							}
																																							else
																																							{
																																								_audioPlayer.HandlePlaySound(play2);
																																							}
																																						}
																																						else
																																						{
																																							_audioPlayer.HandlePlaySound(play);
																																						}
																																						return;
																																					}
																																					Log.Debug("---- Received transaction with {count} messages", transaction.Messages.Count);
																																					foreach (IGameMessage message3 in transaction.Messages)
																																					{
																																						Handle(message3, sender);
																																					}
																																					Log.Debug("---- Finished executing transaction ({count} messages)", transaction.Messages.Count);
																																				}
																																				else if (!IsHost)
																																				{
																																					TurntableReceiver turntableReceiver = TurntableReceiver.ReceiverForTurntableId(turntableUpdateStopIndex.TurntableId);
																																					if (turntableReceiver == null)
																																					{
																																						Log.Error("No receiver for {turntableId}", turntableUpdateStopIndex.TurntableId);
																																					}
																																					else
																																					{
																																						turntableReceiver.HandleUpdateStopIndex(turntableUpdateStopIndex.Tick, turntableUpdateStopIndex.Angle, turntableUpdateStopIndex.StopIndex);
																																					}
																																				}
																																			}
																																			else if (!IsHost)
																																			{
																																				TurntableReceiver turntableReceiver2 = TurntableReceiver.ReceiverForTurntableId(turntableUpdateAngle.TurntableId);
																																				if (turntableReceiver2 == null)
																																				{
																																					Log.Error("No receiver for {turntableId}", turntableUpdateAngle.TurntableId);
																																				}
																																				else
																																				{
																																					turntableReceiver2.HandleUpdateAngle(turntableUpdateAngle.Tick, turntableUpdateAngle.Angle);
																																				}
																																			}
																																		}
																																		else
																																		{
																																			_playersManager.HandleUpdateTrainCrews(updateTrainCrews.TrainCrews);
																																		}
																																	}
																																	else if (IsHost)
																																	{
																																		_playersManager.HandleRequestSetTrainCrewTimetableSymbol(sender, requestSetTrainCrewTimetableSymbol.TrainCrewId, requestSetTrainCrewTimetableSymbol.TimetableSymbol);
																																	}
																																}
																																else if (IsHost)
																																{
																																	_playersManager.HandleRequestRenameTrainCrew(sender, requestEditTrainCrew.TrainCrewId, requestEditTrainCrew.Name, requestEditTrainCrew.Description);
																																}
																															}
																															else if (IsHost)
																															{
																																_playersManager.HandleRequestDeleteTrainCrew(sender, requestDeleteTrainCrew.TrainCrewId);
																															}
																														}
																														else if (IsHost)
																														{
																															_playersManager.HandleRequestCreateTrainCrew(sender, requestCreateTrainCrew.TrainCrew);
																														}
																													}
																													else
																													{
																														_propertyObjectManager.HandlePropertyChange(change);
																													}
																												}
																												else if (IsHost)
																												{
																													EquipmentPurchase.HandleRequest(sender, request2);
																												}
																											}
																											else
																											{
																												_trainController.HandleSetCarTrainCrew(sender, setCarTrainCrew.CarId, setCarTrainCrew.TrainCrewId);
																											}
																										}
																										else
																										{
																											WaitTime(waitTime.Hours);
																										}
																									}
																									else
																									{
																										TimeWeather.Now = new GameDateTime(setTimeOfDay.TimeOfDay);
																										Messenger.Default.Send(default(TimeAdvanced));
																										Console.Log("Time: " + TimeWeather.TimeOfDayString);
																									}
																								}
																								else if (IsHost)
																								{
																									opsController.RequestOps(sender, request);
																								}
																							}
																							else if (IsHost)
																							{
																								opsController.SwitchListController.SetSwitchListCarIds(switchListSetCarIds.TrainCrewId, switchListSetCarIds.CarIds, send: true);
																							}
																						}
																						else if (IsHost)
																						{
																							opsController.SwitchListController.ToggleSwitchListCarIds(switchListToggleCarIds.TrainCrewId, switchListToggleCarIds.CarIds, switchListToggleCarIds.On);
																						}
																					}
																					else if (PlayersManager.MyTrainCrew?.Id == switchListUpdate.TrainCrewId)
																					{
																						Log.Debug("Received switch list with {entries}", switchListUpdate.SwitchList.Entries.Count);
																						SwitchListPanel.Refresh(switchListUpdate.SwitchList);
																						Messenger.Default.Send(default(SwitchListDidChange));
																					}
																				}
																				else
																				{
																					HandleFireEvent(fireEvent.EventCode);
																				}
																			}
																			else
																			{
																				_trainController.HandleCarSetChangeCars(carSetChangeCars.Set);
																			}
																		}
																		else
																		{
																			_trainController.HandleCarSetRemove(carSetRemove.SetId);
																		}
																	}
																	else
																	{
																		_trainController.HandleCarSetAdd(carSetAdd.Set);
																	}
																}
																else
																{
																	_trainController.HandleRemoveCars(message2);
																}
															}
															else
															{
																_trainController.HandleAddCars(message);
															}
														}
														else
														{
															_trainController.HandlePlaceTrain(sender, place);
														}
													}
													else
													{
														_trainController.HandleBatchCarAirUpdate(update2);
													}
												}
												else
												{
													_trainController.HandleBatchCarPositionUpdate(update);
												}
											}
											else
											{
												_trainController.HandleSetBardo(carSetBardo.CarId, carSetBardo.Bardo);
											}
										}
										else
										{
											_trainController.HandleSetIdent(carSetIdent.CarId, new CarIdent(carSetIdent.ReportingMark, carSetIdent.RoadNumber));
										}
									}
									else if (IsHost)
									{
										_trainController.HandleRequestSetIdent(sender, requestCarSetIdent.CarId, new CarIdent(requestCarSetIdent.ReportingMark, requestCarSetIdent.RoadNumber));
									}
								}
								else
								{
									_trainController.HandleRerail(rerail.CarIds, rerail.Amount, sender);
								}
							}
							else
							{
								_trainController.HandleManualMoveCar(manualMoveCar.CarId, manualMoveCar.Direction);
							}
						}
						else
						{
							_trainController.HandleSetGladhandsConnected(setGladhandsConnected.CarIdA, setGladhandsConnected.CarIdB, setGladhandsConnected.Connected);
						}
					}
					else
					{
						_trainController.HandleSetSwitch(setSwitch2);
					}
				}
				else
				{
					_trainController.HandleRequestSetSwitchUnlocked(setSwitchUnlocked, sender);
				}
			}
			else
			{
				_trainController.HandleRequestSetSwitch(setSwitch, sender);
			}
		}
		else
		{
			HandleCharacterMessage(characterMessage, sender);
		}
	}

	public void SendTo(IPlayer player, IGameMessage gameMessage)
	{
		HostManager.Shared.SendTo(player.PlayerId, new GameMessageEnvelope(PlayersManager.PlayerId.String, gameMessage));
	}

	public void SendFireEvent<TEvent>(TEvent evt)
	{
		AssertIsHost();
		int eventCode;
		if (!(evt is BalanceDidChange))
		{
			if (!(evt is ProgressionStateDidChange))
			{
				if (!(evt is RequestRejected))
				{
					if (!(evt is ReputationUpdated))
					{
						throw new ArgumentOutOfRangeException("evt", evt, null);
					}
					eventCode = 3;
				}
				else
				{
					eventCode = 2;
				}
			}
			else
			{
				eventCode = 1;
			}
		}
		else
		{
			eventCode = 0;
		}
		ApplyLocal(new FireEvent(eventCode));
	}

	private IEnumerator SendFireEventDelayed<TEvent>(TEvent evt, float delay)
	{
		yield return new WaitForSecondsRealtime(delay);
		SendFireEvent(evt);
	}

	private void HandleFireEvent(int eventCode)
	{
		Messenger messenger = Messenger.Default;
		switch (eventCode)
		{
		case 0:
			messenger.Send(default(BalanceDidChange));
			break;
		case 1:
			messenger.Send(default(ProgressionStateDidChange));
			break;
		case 2:
			messenger.Send(default(RequestRejected));
			break;
		case 3:
			messenger.Send(default(ReputationUpdated));
			break;
		default:
			throw new ArgumentOutOfRangeException("eventCode", eventCode, null);
		}
	}

	private void HandleCharacterMessage(ICharacterMessage characterMessage, IPlayer sender)
	{
		if (!(characterMessage is AddUpdateCharacter propertyValue))
		{
			if (!(characterMessage is UpdateCharacterPosition updateCharacterPosition))
			{
				if (!(characterMessage is Say say))
				{
					if (!(characterMessage is RequestSetTrainCrewMembership requestSetTrainCrewMembership))
					{
						if (characterMessage is UpdateCameraPosition updateCameraPosition)
						{
							_playersManager.UpdateCameraPosition(updateCameraPosition.Position, sender);
						}
					}
					else if (IsHost)
					{
						_playersManager.HandleRequestTrainCrewMembership(new PlayerId(requestSetTrainCrewMembership.PlayerId), requestSetTrainCrewMembership.TrainCrewId, requestSetTrainCrewMembership.Join);
					}
				}
				else
				{
					Log.Information("Say {PlayerName}, {Text}", sender, say.text);
					Hyperlink hyperlink = Hyperlink.To(sender);
					string str = say.text.Truncate(512);
					Console.Log($"{hyperlink}: {str.ConsoleEscape()}");
				}
			}
			else
			{
				AvatarPose pose = (AvatarPose)updateCharacterPosition.Pose;
				CharacterPosition position = updateCharacterPosition.Position;
				sender.CheckedRemotePlayer().UpdateAvatarPosition(position.Position, position.Forward, position.Look, updateCharacterPosition.Velocity, position.RelativeToCarId, pose, updateCharacterPosition.Tick);
			}
			return;
		}
		Log.Information("AddUpdateCharacter {@Message}", propertyValue);
		RemotePlayer remotePlayer = sender.CheckedRemotePlayer();
		try
		{
			AvatarDescriptor avatarDescriptor = AvatarDescriptor.From(propertyValue.Customization);
			remotePlayer.AddUpdateAvatar(avatarDescriptor);
		}
		catch
		{
			remotePlayer.AddUpdateAvatar(AvatarDescriptor.Default);
		}
	}

	public void RegisterPropertyObject(string id, IKeyValueObject keyValueObject, AuthorizationRequirement requirement, object requirementObject = null)
	{
		RegisterPropertyObject(id, keyValueObject, new AuthorizationRequirementInfo(requirement, requirementObject).StaticDelegate());
	}

	public void RegisterPropertyObject(string id, IKeyValueObject keyValueObject, IPropertyAccessControlDelegate accessControlDelegate)
	{
		_propertyObjectManager.RegisterPropertyObject(id, keyValueObject, accessControlDelegate);
		keyValueObject.OnSetValueLocal = delegate(string key2, Value value3)
		{
			PropagateSetValueLocal(id, value3, key2);
		};
		if (!IsHost)
		{
			return;
		}
		using (TransactionScope())
		{
			foreach (var (key, value2) in keyValueObject.Dictionary)
			{
				PropagateSetValueLocal(id, value2, key);
			}
		}
	}

	public void UnregisterPropertyObject(string id)
	{
		_propertyObjectManager.Unregister(id);
	}

	private static void PropagateSetValueLocal(string objectId, Value value, string key)
	{
		if (Multiplayer.Client == null)
		{
			if (HostManager.Shared != null)
			{
				HostManager.Shared.SetSnapshotProperty(objectId, key, PropertyValueConverter.RuntimeToSnapshot(value));
			}
		}
		else if (Multiplayer.Client.IsClientStatusActive)
		{
			IPropertyValue value2 = PropertyValueConverter.RuntimeToSnapshot(value);
			Multiplayer.Client.Send(new PropertyChange(objectId, key, value2));
		}
	}

	[CanBeNull]
	public static IDisposable TransactionScope()
	{
		if (!(Multiplayer.Client != null))
		{
			return null;
		}
		return Multiplayer.Client.TransactionScope();
	}

	public static void Save(string saveName = null)
	{
		Shared.SaveManager.Save(saveName);
	}

	public void PopulateSnapshotForSave(ref Snapshot snapshot, ref Dictionary<string, PlayerRecord> playerPersistedStates, ref List<SerializableLedgerEntry> ledgerEntries)
	{
		_propertyObjectManager.PopulateSnapshotForSave(ref snapshot);
		_playersManager.PopulateSnapshotForSave(ref snapshot, ref playerPersistedStates);
		opsController.PopulateSnapshotForSave(ref snapshot);
		Ledger.PopulateForSave(ledgerEntries);
	}

	public void PopulateFromRemoteSnapshot(Snapshot snapshot)
	{
		TrainController shared = TrainController.Shared;
		if (shared == null)
		{
			Log.Error("Ignoring RestoreFromSnapshot -- no TrainController");
			return;
		}
		ref Dictionary<string, Snapshot.Player> players = ref snapshot.players;
		if (players == null)
		{
			players = new Dictionary<string, Snapshot.Player>();
		}
		ref Dictionary<string, Snapshot.TrainCrew> trainCrews = ref snapshot.TrainCrews;
		if (trainCrews == null)
		{
			trainCrews = new Dictionary<string, Snapshot.TrainCrew>();
		}
		ref Dictionary<string, SwitchList> switchLists = ref snapshot.SwitchLists;
		if (switchLists == null)
		{
			switchLists = new Dictionary<string, SwitchList>();
		}
		ref Dictionary<string, Snapshot.TurntableState> turntables = ref snapshot.Turntables;
		if (turntables == null)
		{
			turntables = new Dictionary<string, Snapshot.TurntableState>();
		}
		if (snapshot.map.DefaultSpawnPosition.magnitude > 0.001f)
		{
			CameraSelector.shared.DefaultSpawn = new PositionRotation(snapshot.map.DefaultSpawnPosition, Quaternion.Euler(0f, snapshot.map.DefaultSpawnRotationY, 0f));
		}
		using (TransactionScope())
		{
			HandleSnapshotMapFeatures(snapshot.Properties);
			shared.HandleSnapshotSwitches(snapshot.thrownSwitchIds);
			shared.HandleSnapshotTurntables(snapshot.Turntables);
			shared.HandleSnapshotCars(snapshot.Version, snapshot.Cars, snapshot.CarSets, snapshot.CarAir, snapshot.Properties);
			_playersManager.RestoreFromSnapshot(snapshot.players, snapshot.TrainCrews);
			ApplySetupPropertyPresets(snapshot.Properties);
			RestoreProperties(snapshot.Properties);
			ApplySnapshotMap(snapshot.map);
			RestoreNotifier.Shared.NotifyDidRestore();
			shared.PostRestoreProperties();
			RestoreSwitchLists(snapshot.SwitchLists);
			opsController.PostRestoreProperties();
		}
		global::Analytics.Analytics.Post("DidPopulate", new Dictionary<string, object>
		{
			{
				"mode",
				GameMode.DisplayString()
			},
			{
				"numCars",
				snapshot.Cars.Count
			},
			{
				"numCarSets",
				snapshot.CarSets.Count
			},
			{
				"numPlayers",
				snapshot.players.Count
			},
			{
				"numTrainCrews",
				snapshot.TrainCrews.Count
			}
		});
		Multiplayer.UpdateLobbyFlags();
		shared.ShowLostCarCutsWindowIfNeeded();
	}

	private static void HandleSnapshotMapFeatures(Dictionary<string, Dictionary<string, IPropertyValue>> snapshotProperties)
	{
		if (!snapshotProperties.TryGetValue("mapFeatures", out var value))
		{
			value = new Dictionary<string, IPropertyValue>();
		}
		MapFeatureManager.Shared.HandleSnapshotProperties(origin: (!IsHost) ? SetValueOrigin.Remote : SetValueOrigin.Local, properties: PropertyValueConverter.SnapshotToRuntime(value));
	}

	private void ApplySetupPropertyPresets(Dictionary<string, Dictionary<string, IPropertyValue>> properties)
	{
		foreach (var (key, key2, value) in _gameSetupPropertyPresets)
		{
			if (!properties.TryGetValue(key, out var value2))
			{
				value2 = new Dictionary<string, IPropertyValue>();
			}
			value2[key2] = PropertyValueConverter.RuntimeToSnapshot(value);
			properties[key] = value2;
		}
		_gameSetupPropertyPresets.Clear();
	}

	internal void ApplySnapshotMap(Snapshot.Map snapshotMap)
	{
		TimeWeather.Now = new GameDateTime(snapshotMap.Day, snapshotMap.TimeOfDay);
	}

	private void RestoreProperties(Dictionary<string, Dictionary<string, IPropertyValue>> theProperties)
	{
		SetValueOrigin origin = ((!IsHost) ? SetValueOrigin.Remote : SetValueOrigin.Local);
		_propertyObjectManager.RestoreProperties(theProperties, origin);
	}

	private void RestoreSwitchLists(Dictionary<string, SwitchList> switchLists)
	{
		try
		{
			opsController.RestoreSwitchLists(switchLists);
			TrainCrew myTrainCrew = _playersManager.MyTrainCrew;
			if (myTrainCrew != null && !string.IsNullOrEmpty(myTrainCrew.Id) && switchLists.TryGetValue(myTrainCrew.Id, out var value))
			{
				SwitchListPanel.Refresh(value);
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Error restoring switch lists");
		}
	}

	public bool CanAfford(int expense)
	{
		if (GameMode == GameMode.Sandbox)
		{
			return true;
		}
		return Balance >= expense;
	}

	public void ApplyToBalance(int amount, Ledger.Category category, EntityReference? payee, string memo = null, int count = 0, bool quiet = false)
	{
		AssertIsHost();
		if (amount != 0)
		{
			Ledger.Record(amount, category, payee, memo, count, TimeWeather.Now);
			int balance = Balance;
			int num = balance + amount;
			Log.Information("ApplyToBalance: {current} + {amount} = {result}", balance, amount, num);
			Balance = num;
			BalanceDidChange evt = default(BalanceDidChange);
			if (!quiet)
			{
				Multiplayer.Broadcast((amount > 0) ? $"Received payment of {amount:C0}. Balance is now {num:C0}." : $"Sent payment of {amount:C0}. Balance is now {num:C0}.");
			}
			string text = category switch
			{
				Ledger.Category.Passenger => "punch", 
				Ledger.Category.Freight => "stamp", 
				_ => null, 
			};
			if (text != null)
			{
				ScheduledAudioPlayer.HostPlaySoundNotification(text);
				StartCoroutine(SendFireEventDelayed(evt, 1f));
			}
			else
			{
				SendFireEvent(evt);
			}
		}
	}

	public int GetBalance()
	{
		return Balance;
	}

	public IKeyValueObject KeyValueObjectForId(string id)
	{
		return _propertyObjectManager.ObjectForIdOrNull(id);
	}

	private void WaitTime(float hours)
	{
		if (IsHost)
		{
			StartCoroutine(WaitTimeCoroutine(hours));
		}
	}

	private IEnumerator WaitTimeCoroutine(float hours)
	{
		float timeMultiplier = TimeWeather.TimeMultiplier;
		float remaining = hours * 60f * 60f;
		Log.Debug("WaitTime {hours} -> {dt}", hours, remaining);
		IsWaiting = true;
		GameDateTime timeCursor = TimeWeather.Now;
		while (remaining > 0f)
		{
			float num = Mathf.Min(3600f, remaining);
			Industry.TickAll(num / timeMultiplier);
			timeCursor = timeCursor.AddingSeconds(num);
			ApplyLocal(new SetTimeOfDay((float)timeCursor.TotalSeconds));
			remaining -= num;
			yield return new WaitForSeconds(0.25f);
		}
		IsWaiting = false;
		Log.Debug("WaitTime {hours} complete", hours);
	}

	public void ReturnToMainMenu()
	{
		gameManager.ReturnToMainMenu();
	}

	public void ReturnToMainMenuWithError(string title, string message)
	{
		gameManager.ReturnToMainMenu();
		ModalAlertController.PresentOkay(title, message);
	}

	public static void AssertIsHost()
	{
		if (!IsHost)
		{
			throw new Exception("Only host can call");
		}
	}

	public static void DebugAssertIsHost()
	{
	}

	public static bool CheckAuthorizedToSendMessage(IGameMessage message)
	{
		return HostManager.CheckAuthorizedToSendMessage(message, PlayersManager.PlayerId, AccessLevel);
	}

	public static bool CheckAuthorizedToChangeProperty(string id, string key)
	{
		return CheckAuthorizedToSendMessage(new PropertyChange(id, key, default(NullPropertyValue)));
	}

	public bool CheckAuthorizationForPropertyChange(string id, string key, PlayerId senderPlayerId, AccessLevel senderAccessLevel)
	{
		AuthorizationRequirementInfo requirement = _propertyObjectManager.AuthorizationRequirementForPropertyWrite(id, key);
		return SenderSatisfiesAuthorizationRequirement(requirement, senderPlayerId, senderAccessLevel, key);
	}

	private bool SenderSatisfiesAuthorizationRequirement(AuthorizationRequirementInfo requirement, PlayerId senderPlayerId, AccessLevel senderAccessLevel, string key)
	{
		switch (requirement.Requirement)
		{
		case AuthorizationRequirement.HostOnly:
			if (IsHost)
			{
				return senderPlayerId == PlayersManager.PlayerId;
			}
			return false;
		case AuthorizationRequirement.PlayerIdKey:
			if (!IsHost)
			{
				return senderPlayerId.String == key;
			}
			return true;
		case AuthorizationRequirement.MinimumLevelPassenger:
			return senderAccessLevel >= AccessLevel.Passenger;
		case AuthorizationRequirement.MinimumLevelCrew:
		{
			if (senderAccessLevel < AccessLevel.Crew)
			{
				return false;
			}
			if (senderAccessLevel >= AccessLevel.Trainmaster)
			{
				return true;
			}
			if (_storage.TrainCrewMembershipRequired && requirement.Object is string trainCrewId && _playersManager.TrainCrewForId(trainCrewId, out var trainCrew))
			{
				return trainCrew.MemberPlayerIds.Contains(senderPlayerId);
			}
			return true;
		}
		case AuthorizationRequirement.MinimumLevelDispatcher:
			return senderAccessLevel >= AccessLevel.Dispatcher;
		case AuthorizationRequirement.MinimumLevelTrainmaster:
			return senderAccessLevel >= AccessLevel.Trainmaster;
		case AuthorizationRequirement.MinimumLevelOfficer:
			return senderAccessLevel >= AccessLevel.Officer;
		case AuthorizationRequirement.MinimumLevelPresident:
			return senderAccessLevel >= AccessLevel.President;
		default:
			throw new ArgumentOutOfRangeException("requirement", requirement, null);
		}
	}

	public void HostRejectMessage(PlayerId playerId, IGameMessage gameMessage)
	{
		AssertIsHost();
		if (gameMessage is PropertyChange propertyChange)
		{
			_propertyObjectManager.HostHandlePropertyChangeRejected(playerId, propertyChange);
		}
	}

	public void RecordAutoEngineerRunDuration(float seconds)
	{
		_storage.UnbilledAutoEngineerRunDuration += seconds;
	}

	private void PayAutoEngineerWages()
	{
		float unbilledAutoEngineerRunDuration = _storage.UnbilledAutoEngineerRunDuration;
		int num = Mathf.FloorToInt(unbilledAutoEngineerRunDuration / 3600f * 5f);
		float num2 = (float)num / 5f;
		float unbilledAutoEngineerRunDuration2 = unbilledAutoEngineerRunDuration - num2 * 3600f;
		if (num > 0)
		{
			ApplyToBalance(-num, Ledger.Category.WagesAI, null);
			Multiplayer.Broadcast($"Paid {num:C0} for {num2:F1} hours of engineer services.");
			_storage.UnbilledAutoEngineerRunDuration = unbilledAutoEngineerRunDuration2;
		}
	}

	private void OnDayDidChange(TimeDayDidChange _)
	{
		if (IsHost)
		{
			PayAutoEngineerWages();
		}
	}
}
