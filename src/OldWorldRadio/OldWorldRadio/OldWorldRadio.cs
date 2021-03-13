using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class OldWorldRadio : Mod {
	const string BUILD_NAME = "Choerry";
	const string MOD_NAME = "OldWorldRadio";
	const string HARMONY_ID = "my.1bitgodot.oldworldradio";
	const string ITEM_NAME = "OldWorldRadio";
	const string ITEM_DISPLAYNAME = "Old World Radio";
	const string ITEM_DESCRIPTION = "Old yet quite sophisticated.";
	const int ITEM_ID = 26913;
	const float AUDIO_DEFAULT_VOLUME = 0.25f;
	const float AUDIO_MIN_DISTANCE = 0.7f;
	const float AUDIO_MAX_DISTANCE = 40f;
	const float AUDIO_ECHO_WETMIX = 0.2f;
	const float AUDIO_ECHO_DELAY = 125f;	

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
	bool radioOn;
	HNotify notify;
	HNotification notifyLoad;
	AssetBundle assetBundle;
	GameObject audioGameObject;
	AudioEchoFilter audioEchoFilter;
	AudioSource audioSource;
	List<string> audioClips;
	float audioVolume = AUDIO_DEFAULT_VOLUME;
	int audioTrack;
	bool broadcast;


	void Log(string text, LogType type) {
		Debug.Log($"[{MOD_NAME}] {type}: {text}");
	}


	void Info(string text) { Log(text, LogType.Info); }
	void Warning(string text) { Log(text, LogType.Warning); }
	void Error(string text) { Log(text, LogType.Error); }


	bool CreateRadioItem() {
		if (radio != null) return false;
		radio = ItemManager.GetItemByIndex(ITEM_ID);
		if (radio != null) { Error($"{ITEM_DISPLAYNAME} item already created."); return false; }
		var originalRadio = ItemManager.GetItemByName("Placeable_Radio");
		if (originalRadio == null) { Error("Cannot find original radio item."); return false; }
		radio = Object.Instantiate(originalRadio); radio.name = ITEM_NAME;
		var traverse = Traverse.Create(radio);
		traverse.Property("UniqueName").SetValue(ITEM_NAME);
		traverse.Property("UniqueIndex").SetValue(ITEM_ID);
		traverse.Field("settings_Inventory").Property("DisplayName").SetValue(ITEM_DISPLAYNAME);
		traverse.Field("settings_Inventory").Property("Description").SetValue(ITEM_DESCRIPTION);
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


	bool UpdateRadioState() {
		if (player == null) return false;
		var status = false;
		if (!status) {
			var slot = GetRadioSlot();
			status = (slot != null && slot.slotType == SlotType.Hotbar);
		}
		if (status == radioOn) return false;
		audioSource.volume = (radioOn = status) ? audioVolume : 0;
		Info(radioOn ?
			$"{ITEM_DISPLAYNAME} is switched on." :
			$"{ITEM_DISPLAYNAME} is switched off.");
		return true;
	}


	bool GiveRadioToPlayer() {
		if (player == null) return false;
		if (GetRadioSlot() != null) {
			Error($"Player already has {ITEM_DISPLAYNAME}.");
			radioOn = false;
			return false;
		}
	//	RAPI.GiveItem(radio, 1);
		player.Inventory.AddItem(ITEM_NAME, 1);
		dropped = false;
		Info($"Gave {ITEM_DISPLAYNAME} to player.");
		return true;
	}


	bool TakeRadioFromPlayer() {
		if (player == null) return false;
		var slot = GetRadioSlot();
		if (slot == null) { Error($"Player does not have {ITEM_DISPLAYNAME}."); return false; }
		slot.RemoveItem(1); Info($"Took {ITEM_DISPLAYNAME} from player."); return true;
	}


	bool SetBroadcastState(bool state) {
		if (player == null) return false;
		if (broadcast == state) return false;
		broadcast = state;
		if (state) {
			Broadcast();
			Info("Broadcasting started.");
		} else {
			audioSource.Stop();
			Info("Broadcasting stopped.");
		}
		return true;
	}

	bool Broadcast() {
		if (player == null) return false;
		if (!broadcast) return false;
		audioTrack = Random.Range(0, audioClips.Count - 1);
		var audioClip = assetBundle.LoadAsset<AudioClip>(audioClips[audioTrack]);
		audioSource.clip = audioClip; audioSource.Play();
		Invoke("Broadcast", audioClip.length + .5f);
		return true;
	}


	bool RemoveAudioSource() {
		if (player == null) return false;
		if (audioGameObject == null) {
			Error("Audio source does not exist.");
			return false;
		}
		Destroy(audioGameObject);
		audioGameObject = null;
		audioEchoFilter = null;
		audioSource = null;
		dropped = false;
		Info("Audio source removed.");
		return true;
	}

	bool CreateAudioSource() {
		if (player == null) return false;
		if (audioGameObject != null) {
			Error("Audio source already exists.");
			return false;
		}
		audioGameObject = new GameObject();
		audioSource = audioGameObject.AddComponent<AudioSource>();
		audioEchoFilter = audioGameObject.AddComponent<AudioEchoFilter>();
		audioEchoFilter.wetMix = AUDIO_ECHO_WETMIX;
		audioEchoFilter.delay = AUDIO_ECHO_DELAY;
		audioSource.rolloffMode = AudioRolloffMode.Linear;
		audioSource.minDistance = AUDIO_MIN_DISTANCE;
		audioSource.maxDistance = AUDIO_MAX_DISTANCE;
		audioSource.bypassEffects = true;
		audioSource.volume = 0;
		dropped = false;
		Info("Audio source created.");
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
		Info($"Assets loaded to {assetBundle.name}");
		var assetNames = new List<string>(assetBundle.GetAllAssetNames());
		audioClips = assetNames.FindAll(a => a.EndsWith(".ogg"));
		player = RAPI.GetLocalPlayer();
		CreateRadioItem();
		CreateAudioSource();
		GiveRadioToPlayer();
		SetBroadcastState(true);
		notifyLoad.Close();
		notifyLoad = null;
		yield return true;
	}


	public void Start() {
		instance = this;
		harmony = new Harmony(HARMONY_ID);
		harmony.PatchAll(Assembly.GetExecutingAssembly());
		StartCoroutine(OnModLoad());
		Info($"Mod build {BUILD_NAME} loaded.");
	}


	public void Update() {
		UpdateRadioState();
	}


	public void OnModUnload() {
		TakeRadioFromPlayer();
		SetBroadcastState(false);
		RemoveAudioSource();
		assetBundle.Unload(true);
		Info("Mod unloaded.");
	}


	public override void WorldEvent_WorldLoaded() {
		base.WorldEvent_WorldLoaded();
		player = RAPI.GetLocalPlayer();
		CreateAudioSource();
		GiveRadioToPlayer();
		SetBroadcastState(true);
	}


	public override void WorldEvent_WorldUnloaded() {
		base.WorldEvent_WorldUnloaded();
		TakeRadioFromPlayer();
		SetBroadcastState(false);
		RemoveAudioSource();
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


	[ConsoleCommand("take_radio", "Remove radio from player inventory.")]
	public static string TakeRadioCommand(string[] args) {
		var result = instance.TakeRadioFromPlayer();
		return "take_radio " + (result ? "success." : "failed.");
	}


	[ConsoleCommand("give_radio", "Add radio to player inventory.")]
	public static string GiveRadioCommand(string[] args) {
		var result = instance.GiveRadioToPlayer();
		return "give_radio " + (result ? "success." : "failed.");
	}

	[ConsoleCommand("radio_volume", "Get and set the radio volume.")]
	public static string RadioVolumeCommand(string[] args) {
		float value = instance.audioVolume * 100;
		if (args.Length == 0) return $"{ITEM_DISPLAYNAME} volume is at {value:N0}%";
		var argument = args[0].Trim(); if (argument.EndsWith("%"))
			argument = argument.Substring(0, argument.Length - 1);
		if (!float.TryParse(argument, out value) || value < 0 || value > 100)
			return "Volume must be a value from 0 to 100.";
		instance.audioVolume = value / 100;
		instance.audioSource.volume = instance.audioVolume;
		return $"Radio volume set to {value:N0}%";
	}

	[ConsoleCommand("radio_debug", "For internal use only.")]
	public static string RadioDebugCommand(string [] args) {
		var x = instance;
		if (x.player != null) {
			var v1 = x.player.transform.position;
			x.Info($"Player position: X={v1.x},Y={v1.y},Z={v1.z}");
		}
		if (x.gameObject != null) {
			var v1 = x.gameObject.transform.position;
			x.Info($"GameObject position: X={v1.x},Y={v1.y},Z={v1.z}");
		}
		if (x.audioGameObject != null) {
			var v1 = x.audioGameObject.transform.position;
			x.Info($"AudioGameObject position: X={v1.x},Y={v1.y},Z={v1.z}");
		}
		return string.Empty;
	}

}