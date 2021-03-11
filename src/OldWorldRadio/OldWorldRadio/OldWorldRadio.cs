using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class OldWorldRadio : Mod {
	const string MOD_NAME = "OldWorldRadio";
	const string HARMONY_ID = "my.1bitgodot.oldworldradio";
	const string ITEM_NAME = "OldWorldRadio";
	const string ITEM_DISPLAYNAME = "Old World Radio";
	const string ITEM_DESCRIPTION = "Old yet quite sophisticated.";
	const int ITEM_ID = 26913;
	const float DEFAULT_AUDIO_VOLUME = 0.75f;


	enum LogType {
		Info,
		Warning,
		Error
	}


	static OldWorldRadio instance;
	Harmony harmony;
	Item_Base radio;
	Network_Player player;
	Slot radioSlot;
	bool dropped;
	bool holding;
	HNotify notify;
	HNotification notifyLoad;
	AssetBundle assetBundle;
	AudioSource audioSource;
	List<string> audioClips;
	float audioVolume = DEFAULT_AUDIO_VOLUME;
	int audioTrack;


	void Log(string text, LogType type) {
		Debug.Log($"[{MOD_NAME}] {type}: {text}");
	}


	void Info(string text) { Log(text, LogType.Info); }
	void Warning(string text) { Log(text, LogType.Warning); }
	void Error(string text) { Log(text, LogType.Error); }


	bool CreateRadioItem() {
		if (radio != null) return false;
		radio = ItemManager.GetItemByIndex(ITEM_ID);
		if (radio != null) { Info($"{ITEM_DISPLAYNAME} item already created."); return false; }
		var originalRadio = ItemManager.GetItemByName("Placeable_Radio");
		if (originalRadio == null) { Error("Cannot find original radio item."); return false; }
		radio = Object.Instantiate(originalRadio); radio.name = ITEM_NAME;
		var traverse = Traverse.Create(radio);
		traverse.Property("UniqueName").SetValue(ITEM_NAME);
		traverse.Property("UniqueIndex").SetValue(ITEM_ID);
		traverse.Field("settings_Inventory").Property("DisplayName").SetValue(ITEM_DISPLAYNAME);
		traverse.Field("settings_Inventory").Property("Description").SetValue(ITEM_DESCRIPTION);
//		RAPI.AddItemToBlockQuadType(radio, RBlockQuadType.quad_floor);
//		RAPI.AddItemToBlockQuadType(radio, RBlockQuadType.quad_foundation);
//		RAPI.AddItemToBlockQuadType(radio, RBlockQuadType.quad_table);
		RAPI.RegisterItem(radio);
		Info($"{ITEM_DISPLAYNAME} item created.");
		return true;
	}


	bool IsRadioSlot(Slot slot) {
		return slot != null && !slot.IsEmpty &&
			slot.GetItemBase().UniqueIndex == ITEM_ID;
	}


	Slot GetRadioSlot() { 
		if (player == null) return null;
		if (IsRadioSlot(radioSlot)) return radioSlot;
		var slots = player.Inventory.allSlots;
		foreach (var slot in slots) {
			if (IsRadioSlot(slot))
				return radioSlot = slot;
		}
		radioSlot = null;
		return null;
	}


	void UpdateRadioState() {
		var slot = GetRadioSlot();
		var status = (slot != null && slot.slotType == SlotType.Hotbar);
		if (status == holding) return;
		audioSource.volume = (holding = status) ? audioVolume : 0;
		Info(holding ?
			$"{ITEM_DISPLAYNAME} is switched on."  :
			$"{ITEM_DISPLAYNAME} is switched off.");
	}


	bool GiveRadioToPlayer() {
		if (player == null) player = RAPI.GetLocalPlayer();
		if (player == null) return false;
		if (GetRadioSlot() != null) {
			Info($"Player already has {ITEM_DISPLAYNAME}.");
			return false;
		}
		//	RAPI.GiveItem(radio, 1);
		player.Inventory.AddItem(ITEM_NAME, 1);
		dropped = false;
		Info($"Gave {ITEM_DISPLAYNAME} to player.");
		return true;
	}


	bool TakeRadioFromPlayer() {
		var slot = GetRadioSlot();
		if (slot == null) { Info($"Player does not have {ITEM_DISPLAYNAME}."); return false; }
		slot.RemoveItem(1); Info($"Took {ITEM_DISPLAYNAME} from player."); return true;
	}


	bool StartBroadcast() {
		if (audioSource == null) return false;
		audioTrack = Random.Range(0, audioClips.Count - 1);
		var audioClip = assetBundle.LoadAsset<AudioClip>(audioClips[audioTrack]);
		audioSource.clip = audioClip; audioSource.Play();
		Invoke("StartBroadcast", audioClip.length + .5f);
		return true;
	}

	bool StopBroadcast() {
		audioSource.Stop();
		audioSource = null;
		Destroy(audioSource);
		return true;
	}

	IEnumerator OnModLoad() {
		if (notifyLoad != null) notifyLoad.Close();
		if (notify == null) notify = FindObjectOfType<HNotify>();
		yield return notifyLoad = notify.AddNotification(
			HNotify.NotificationType.spinning, $"Loading {MOD_NAME}");
		var request = AssetBundle.LoadFromMemoryAsync(
			GetEmbeddedFileBytes("oldworldradio.assets"));
		yield return request;
		assetBundle = request.assetBundle;
		var assetNames = new List<string>(assetBundle.GetAllAssetNames());
		audioClips = assetNames.FindAll(a => a.EndsWith(".ogg"));
		audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.volume = 0;
		StartBroadcast();
		CreateRadioItem();
		GiveRadioToPlayer();
		yield return new WaitForSeconds(2f);
		notifyLoad.Close();
		notifyLoad = null;
	}


	public void Start() {
		instance = this;
		harmony = new Harmony(HARMONY_ID);
		harmony.PatchAll(Assembly.GetExecutingAssembly());
		StartCoroutine(OnModLoad());
		Info("Mod loaded.");
	}

	public void Update() {
		UpdateRadioState();
	}


	public void OnModUnload() {
		TakeRadioFromPlayer();
		StopBroadcast();
		assetBundle.Unload(true);
		Info("Mod unloaded.");
	}


	public override void WorldEvent_WorldLoaded() {
		base.WorldEvent_WorldLoaded();
		GiveRadioToPlayer();
	}


	public override void WorldEvent_WorldUnloaded() {
		base.WorldEvent_WorldUnloaded();
		TakeRadioFromPlayer();
		player = null;
	}


	public override void LocalPlayerEvent_DropItem(ItemInstance item, Vector3 position, Vector3 direction, bool parentedToRaft) {
		base.LocalPlayerEvent_DropItem(item, position, direction, parentedToRaft);
		if (item.UniqueIndex != ITEM_ID) return;
		Info($"{ITEM_DISPLAYNAME} dropped.");
		dropped = true;
	}


	public override void LocalPlayerEvent_PickupItem(PickupItem item) {
		base.LocalPlayerEvent_PickupItem(item);
		if (item.itemInstance.UniqueIndex == ITEM_ID) {
			if (!dropped) TakeRadioFromPlayer();
			Info($"{ITEM_DISPLAYNAME} picked up.");
			dropped = false;
		}
	}


	[ConsoleCommand("take_radio","Remove radio from player inventory.")]
	public static string TakeRadioCommand(string [] args) {
		var result = instance.TakeRadioFromPlayer();
		return "take_radio " + (result ? "success." : "failed.");
	}


	[ConsoleCommand("give_radio", "Add radio to player inventory.")]
	public static string GiveRadioCommand(string [] args) {
		var result = instance.GiveRadioToPlayer();
		return "give_radio " + (result ? "success." : "failed.");
	}

	[ConsoleCommand("radio_volume", "Get and set the radio volume.")]
	public static string RadioVolumeCommand(string [] args) {
		float value = instance.audioVolume * 100;
		if (args.Length == 0) return $"{ITEM_DISPLAYNAME} volume is at {value:N0}%";
		var argument = args[0].Trim(); if (argument.EndsWith("%"))
			argument = argument.Substring(0,argument.Length - 1);
		if (!float.TryParse(argument, out value) || value < 0 || value > 100)
			return "Volume must be a value from 0 to 100.";
		instance.audioVolume = value / 100;
		instance.audioSource.volume = instance.audioVolume;
		return $"Radio volume set to {value:N0}%";
	}


	/*



		private int track;

		public void StartAudio() {
			if (audioSource != null) {
				var name = trackNames[track];
				var displayName = name.Substring(9 + 7);
				var clip = assetBundle.LoadAsset<AudioClip>(name);
				if (radioState) Debug.Log($"[{MOD_NAME}] Info: {ITEM_NAME} now playing '{displayName}'.");
				audioSource.clip = clip; audioSource.Play();
				if (++track >= trackNames.Length) track = 0;
				Invoke("StartAudio", clip.length + 0.5f);
			}
		}


		public void CloseAudio() {
			audioSource.Stop();
			Destroy(audioSource);
			audioSource = null;
		}

		public void SwitchOnRadio() {
			if (audioSource != null) {
				audioSource.volume = 0.5f; radioState = true;
				Debug.Log($"[{MOD_NAME}] Info: {ITEM_NAME} has been switched on.");
			}
		}


		public void SwitchOffRadio() {
			if (audioSource != null) {
				audioSource.volume = 0.0f; radioState = false;
				Debug.Log($"[{MOD_NAME}] Info: {ITEM_NAME} has been switched off.");
			}
		}


		public bool HasItemBeenMoved() {
			if (player == null || itemSlot == null) return false;
			var item = itemSlot.GetItemBase();
			if (item == null || item.UniqueIndex != ITEM_ID) {
				var oldSlot = itemSlot;
				var newSlot = GetItemInventorySlot();
				if (newSlot != null) { 
					if (oldSlot.slotType == SlotType.Hotbar && newSlot.slotType != SlotType.Hotbar) SwitchOffRadio(); else
					if (oldSlot.slotType != SlotType.Hotbar && newSlot.slotType == SlotType.Hotbar) SwitchOnRadio();
				}
				return true;
			}
			return false;
		}


		public void Update() {
			if (HasItemBeenMoved()) {
				Debug.Log($"[{MOD_NAME}] Info: {ITEM_NAME} has been moved to another slot.");
				Debug.Log($"[{MOD_NAME}] Info: {ITEM_NAME} is in the {itemSlot.slotType}.");
			}
		}

		private IEnumerator OnModLoading() {
			 = FindObjectOfType<HNotify>().AddNotification(HNotify.NotificationType.spinning, $"Loading {MOD_NAME} ...");
			Debug.Log($"[{MOD_NAME}] Info: {trackNames.Length} songs loaded.");
		//	Randomize starting track
			track = Random.Range(0, trackNames.Length - 1);
			audioSource = gameObject.GetComponent<AudioSource>();
			if (audioSource == null) audioSource =
				gameObject.AddComponent<AudioSource>();
			audioSource.volume = 0;
			notification.Close();
			player = RAPI.GetLocalPlayer();
			GiveRadioToPlayer();
		}


		public void OnModUnload() {
			if (carrying) RemoveItemFromPlayer();
			assetBundle.Unload(true);
			Debug.Log($"Mod {MOD_NAME} unloaded.");
		}
	*/
}