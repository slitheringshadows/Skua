﻿using Skua.Core.Utils;
using Skua.Core.Models.Quests;
using Skua.Core.Interfaces;
using Skua.Core.Models;
using Skua.Core.Models.Items;
using Skua.Core.Flash;

namespace Skua.Core.Scripts;

public partial class ScriptQuest : IScriptQuest
{
    private readonly Lazy<IFlashUtil> _lazyFlash;
    private readonly Lazy<IScriptWait> _lazyWait;
    private readonly Lazy<IScriptOption> _lazyOptions;
    private readonly Lazy<IScriptPlayer> _lazyPlayer;
    private readonly Lazy<IScriptMap> _lazyMap;
    private readonly Lazy<IScriptCombat> _lazyCombat;
    private readonly Lazy<IScriptSend> _lazySend;
    private readonly Lazy<IScriptInventory> _lazyInventory;
    private IFlashUtil Flash => _lazyFlash.Value;
    private IScriptWait Wait => _lazyWait.Value;
    private IScriptOption Options => _lazyOptions.Value;
    private IScriptPlayer Player => _lazyPlayer.Value;
    private IScriptMap Map => _lazyMap.Value;
    private IScriptCombat Combat => _lazyCombat.Value;
    private IScriptSend Send => _lazySend.Value;
    private IScriptInventory Inventory => _lazyInventory.Value;

    public ScriptQuest(
        Lazy<IFlashUtil> flash,
        Lazy<IScriptWait> wait,
        Lazy<IScriptOption> options,
        Lazy<IScriptMap> map,
        Lazy<IScriptPlayer> player,
        Lazy<IScriptCombat> combat,
        Lazy<IScriptSend> send,
        Lazy<IScriptInventory> inventory)
    {
        _lazyFlash = flash;
        _lazyWait = wait;
        _lazyOptions = options;
        _lazyPlayer = player;
        _lazyMap = map;
        _lazyCombat = combat;
        _lazySend = send;
        _lazyInventory = inventory;
    }

    private Thread? QuestThread;
    private CancellationTokenSource? QuestsCTS;
    private readonly List<int> _add = new();
    private readonly List<int> _rem = new();

    public int RegisterCompleteInterval { get; set; } = 2000;
    [ObjectBinding("world.questTree", Default = "new()")]
    private Dictionary<int, Quest> _quests = new();
    public List<Quest> Tree => Quests.Values.ToList() ?? new();
    public List<Quest> Active => Tree.FindAll(x => x.Active);
    public List<Quest> Completed => Tree.FindAll(x => x.Status == "c");
    public List<QuestData> Cached { get; set; } = new();
    public List<int> Registered { get; } = new();

    public void Load(params int[] ids)
    {
        if(ids.Length < 30)
        {
            Flash.CallGameFunction("world.showQuests", ids.Select(id => id.ToString()).Join(','), "q");
            return;
        }
        foreach(int[] idchunk in ids.Chunk(30))
        {
            Flash.CallGameFunction("world.showQuests", idchunk.Select(id => id.ToString()).Join(','), "q");
        }
    }

    public Quest EnsureLoad(int id)
    {
        Wait.ForTrue(() => Tree.Contains(x => x.ID == id), () => Load(id), 20);
        return Tree.Find(q => q.ID == id)!;
    }

    public bool TryGetQuest(int id, out Quest? quest)
    {
        return (quest = Tree.Find(x => x.ID == id)) is not null;
    }

    public bool Accept(int id)
    {
        if (Options.SafeTimings)
            Wait.ForActionCooldown(GameActions.AcceptQuest);
        Flash.CallGameFunction("world.acceptQuest", id);
        if (Options.SafeTimings)
            Wait.ForQuestAccept(id);
        return IsInProgress(id);
    }

    public void Accept(params int[] ids)
    {
        for(int i = 0; i < ids.Length; i++)
        {
            Accept(ids[i]);
            Thread.Sleep(Options.ActionDelay);
        }
    }

    public bool EnsureAccept(int id)
    {
        for (int i = 0; i < Options.QuestAcceptAndCompleteTries; i++)
        {
            Accept(id);
            if (IsInProgress(id))
                break;
            Thread.Sleep(Options.ActionDelay);
        }
        return IsInProgress(id);
    }

    public void EnsureAccept(params int[] ids)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            EnsureAccept(ids[i]);
            Thread.Sleep(Options.ActionDelay);
        }
    }

    public bool Complete(int id, int itemId = -1, bool special = false)
    {
        if (Options.SafeTimings)
            Wait.ForActionCooldown(GameActions.TryQuestComplete);
        if (Options.ExitCombatBeforeQuest && Player.InCombat)
            Map.Jump(Player.Cell, Player.Pad);
        Flash.CallGameFunction("world.tryQuestComplete", id, itemId, special);
        if (Options.SafeTimings)
            Wait.ForQuestComplete(id);
        return !IsInProgress(id);
    }

    public void Complete(params int[] ids)
    {
        for(int i = 0; i < ids.Length; i++)
        {
            Complete(ids[i]);
            Thread.Sleep(Options.ActionDelay);
        }
    }

    public bool EnsureComplete(int id, int itemId = -1, bool special = false)
    {
        if (Options.ExitCombatBeforeQuest)
            Combat.Exit();
        _EnsureComplete(id, itemId, special);
        return !IsInProgress(id);
    }

    private void _EnsureComplete(int id, int itemId = -1, bool special = false)
    {
        for (int i = 0; i < Options.QuestAcceptAndCompleteTries; i++)
        {
            Complete(id, itemId, special);
            if (IsInProgress(id))
                break;
            Thread.Sleep(Options.ActionDelay);
        }
    }

    public void EnsureComplete(params int[] ids)
    {
        for(int i = 0; i < ids.Length; i++)
        {
            EnsureComplete(ids[i]);
            Thread.Sleep(Options.ActionDelay);
        }
    }

    [MethodCallBinding("world.isQuestInProgress", GameFunction = true)]
    private bool _isInProgress(int id) => false;

    public bool UpdateQuest(int id)
    {
        Quest? quest = EnsureLoad(id);
        if(quest is null)
            return false;
        Send.ClientPacket("{\"t\":\"xt\",\"b\":{\"r\":-1,\"o\":{\"cmd\":\"updateQuest\",\"iValue\":" + quest.Value + ",\"iIndex\":" + quest.Slot + "}}}", "json");
        return true;
    }

    public void UpdateQuest(int value, int slot)
    {
        Send.ClientPacket("{\"t\":\"xt\",\"b\":{\"r\":-1,\"o\":{\"cmd\":\"updateQuest\",\"iValue\":" + value + ",\"iIndex\":" + slot + "}}}", "json");
    }

    public bool CanComplete(int id)
    {
        return Completed.Contains(q => q.ID == id);
    }

    public bool CanCompleteFullCheck(int id)
    {
        if (CanComplete(id))
            return true;

        Quest? quest = Tree.FirstOrDefault(q => q.ID == id);
        if (quest is null)
            return false;
        List<ItemBase> requirements = new();
        requirements.AddRange(quest.Requirements);
        requirements.AddRange(quest.AcceptRequirements);
        if (requirements.Count == 0)
            return true;
        foreach (ItemBase item in requirements)
        {
            if (Inventory.Contains(item.Name, item.Quantity))
                continue;
            return false;
        }
        return true;
    }

    public bool IsDailyComplete(int id)
    {
        Quest? quest = EnsureLoad(id);
        if (quest is null)
            return false;
        return Flash.CallGameFunction<int>("world.getAchievement", quest.Field, quest.Index) != 0;
    }

    public bool IsUnlocked(int id)
    {
        Quest? quest = EnsureLoad(id);
        if (quest is null)
            return false;
        return quest.Slot < 0 || Flash.CallGameFunction<int>("world.getQuestValue", quest.Slot) >= quest.Value - 1;
    }

    public bool HasBeenCompleted(int id)
    {
        Quest? quest = EnsureLoad(id);
        if (quest is null)
            return false;
        return quest.Slot < 0 || Flash.CallGameFunction<int>("world.getQuestValue", quest.Slot) >= quest.Value;
    }

    public bool IsAvailable(int id)
    {
        Quest? quest = EnsureLoad(id);
        return quest is not null
               && !IsDailyComplete(quest.ID)
               && IsUnlocked(quest.ID)
               && (!quest.Upgrade || Player.Upgrade)
               && Player.Level >= quest.Level
               && (quest.RequiredClassID <= 0 || Flash.CallGameFunction<int>("world.myAvatar.getCPByID", quest.RequiredClassID) >= quest.RequiredClassPoints)
               && (quest.RequiredFactionId <= 1 || Flash.CallGameFunction<int>("world.myAvatar.getRep", quest.RequiredFactionId) >= quest.RequiredFactionRep)
               && quest.AcceptRequirements.All(r => Inventory.Contains(r.Name, r.Quantity));
    }

    public void RegisterQuests(params int[] ids)
    {
        lock (_add)
            _add.AddRange(ids);
        if (!QuestThread?.IsAlive ?? true)
        {
            QuestThread = new(() =>
            {
                QuestsCTS = new();
                Poll(QuestsCTS.Token);
                QuestsCTS.Dispose();
                QuestsCTS = null;
            })
            {
                Name = "Quest Thread"
            };
            QuestThread.Start();
        }
    }

    public void UnregisterQuests(params int[] ids)
    {
        lock (_rem)
            _rem.AddRange(ids);
    }

    public void ClearRegisteredQuests()
    {
        lock (_rem)
            _rem.AddRange(Registered);
        if (QuestThread?.IsAlive ?? false)
        {
            QuestsCTS?.Cancel();
            Wait.ForTrue(() => QuestsCTS is null, 20);
        }
    }

    private void Poll(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_add.Count > 0)
            {
                Registered.AddRange(_add.Except(Registered));
                lock (_add)
                    _add.Clear();
            }
            if (_rem.Count > 0)
            {
                Registered.RemoveAll(_rem.Contains);
                lock (_rem)
                    _rem.Clear();
            }
            if (Player.Playing)
            {
                _CompleteQuest(Registered);
            }
            if (!token.IsCancellationRequested)
                Thread.Sleep(RegisterCompleteInterval);
        }
    }

    private void _CompleteQuest(List<int> registered)
    {
        for (int i = 0; i < registered.Count; i++)
        {
            if (!CanComplete(registered[i]))
                continue;
            if (Options.SafeTimings)
                Wait.ForActionCooldown(GameActions.TryQuestComplete);
            Flash.CallGameFunction("world.tryQuestComplete", registered[i], -1, false);
            if (Options.SafeTimings)
                Wait.ForQuestComplete(registered[i]);
            Accept(registered[i]);
            Thread.Sleep(Options.ActionDelay);
        }
    }
}
