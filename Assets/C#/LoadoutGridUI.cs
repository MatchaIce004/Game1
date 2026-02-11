using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoadoutGridUI : MonoBehaviour
{
    [Header("Data")]
    public ItemDatabase database;

    [Header("Grid")]
    public Transform gridParent;
    public GameObject itemSlotPrefab;

    [Header("Detail Panel")]
    public Image detailIcon;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDesc;

    [Header("Config")]
    public int columns = 5;
    public int rows = 4;
    public int maxLoadout = 3;

    [Header("Locked Visual")]
    public Sprite unknownIconSprite; // 黒塗り用や？アイコン用

    [Header("Confirm Modal")]
    public Button decideButton;
    public GameObject confirmModalRoot;
    public Image[] confirmSlots = new Image[3];
    public Button modalBackButton;
    public Button modalStartButton;
    public Sprite emptySlotSprite;

    private HashSet<string> selectedIds = new HashSet<string>();
    private Dictionary<string, LoadoutSlotView> slotViews = new Dictionary<string, LoadoutSlotView>();
    private bool isModalOpen = false;

    void Start()
    {
        if (database == null)
        {
            Debug.LogError("LoadoutGridUI: ItemDatabaseが未設定です");
            return;
        }
        if (gridParent == null)
        {
            Debug.LogError("LoadoutGridUI: gridParentが未設定です");
            return;
        }
        if (itemSlotPrefab == null)
        {
            Debug.LogError("LoadoutGridUI: itemSlotPrefabが未設定です");
            return;
        }

        if (confirmModalRoot != null)
            confirmModalRoot.SetActive(false);

        if (decideButton != null)
            decideButton.onClick.AddListener(OpenConfirmModal);

        if (modalBackButton != null)
            modalBackButton.onClick.AddListener(CloseConfirmModal);

        if (modalStartButton != null)
            modalStartButton.onClick.AddListener(StartGameFromModal);

        BuildGrid();
        ClearDetail();
    }

    void BuildGrid()
    {
        foreach (Transform c in gridParent) Destroy(c.gameObject);
        slotViews.Clear();
        selectedIds.Clear();

        int total = columns * rows;

        if (ItemManager.Instance == null)
        {
            Debug.LogError("LoadoutGridUI: ItemManager.Instance がいません");
            return;
        }

        // 候補20枠：ItemManagerが作ったIDリスト
        var ids = ItemManager.Instance.runCandidateIds
            .Where(id => !string.IsNullOrEmpty(id))
            .Take(total)
            .ToList();

        var items = ids
            .Select(id => ItemManager.Instance.FindByID(id))
            .Where(x => x != null)
            .ToList();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var go = Instantiate(itemSlotPrefab, gridParent);

            var view = new LoadoutSlotView(go);
            slotViews[item.id] = view;

            bool owned = IsOwned(item);
            bool revealedThisRun = IsRevealedThisRun(item);

            // 表示ルール：
            // - 所持してれば最初から見える
            // - 所持してないなら、入手（このランで発見）するまで黒塗り
            bool visible = owned || revealedThisRun;

            view.SetIcon(visible ? item.icon : unknownIconSprite);
            view.SetLocked(!visible);
            view.SetSelected(false);
            view.SetInteractable(true);

            view.button.onClick.AddListener(() =>
            {
                bool ownedNow = IsOwned(item);
                bool revealedNow = IsRevealedThisRun(item);
                bool visibleNow = ownedNow || revealedNow;
                OnClickItem(item, visibleNow, ownedNow);
            });
        }
    }

    bool IsRevealedThisRun(ItemData item)
    {
        return ItemManager.Instance != null &&
               ItemManager.Instance.runDiscoveredIds != null &&
               ItemManager.Instance.runDiscoveredIds.Contains(item.id);
    }

    bool IsOwned(ItemData item)
    {
        return ItemManager.Instance != null &&
               ItemManager.Instance.ownedPermanent.Any(x => x != null && x.id == item.id);
    }

    void OnClickItem(ItemData item, bool visible, bool owned)
    {
        if (isModalOpen) return;

        if (!visible)
        {
            ShowUnknownDetail();
            return;
        }

        ShowDetail(item);

        // 所持してないなら選択不可（閲覧のみ）
        if (!owned) return;

        if (selectedIds.Contains(item.id))
        {
            selectedIds.Remove(item.id);
            slotViews[item.id].SetSelected(false);
        }
        else
        {
            if (selectedIds.Count >= maxLoadout)
            {
                Debug.Log($"持ち込みは最大 {maxLoadout} 個までです");
                return;
            }
            selectedIds.Add(item.id);
            slotViews[item.id].SetSelected(true);
        }
    }

    void ShowDetail(ItemData item)
    {
        if (detailIcon != null) detailIcon.sprite = item.icon;
        if (detailName != null) detailName.text = item.itemName;
        if (detailDesc != null) detailDesc.text = item.description;
    }

    void ShowUnknownDetail()
    {
        if (detailIcon != null) detailIcon.sprite = null;
        if (detailName != null) detailName.text = "???";
        if (detailDesc != null) detailDesc.text = "まだ手に入れていないアイテムです。";
    }

    void ClearDetail()
    {
        if (detailIcon != null) detailIcon.sprite = null;
        if (detailName != null) detailName.text = "";
        if (detailDesc != null) detailDesc.text = "";
    }

    // =========================
    // Confirm Modal
    // =========================
    void OpenConfirmModal()
    {
        if (confirmModalRoot == null) return;
        if (ItemManager.Instance == null) return;

        isModalOpen = true;

        var selectedItems = selectedIds
            .Select(id => ItemManager.Instance.FindByID(id))
            .Where(x => x != null)
            .Take(maxLoadout)
            .ToList();

        for (int i = 0; i < confirmSlots.Length; i++)
        {
            if (confirmSlots[i] == null) continue;

            if (i < selectedItems.Count)
            {
                confirmSlots[i].sprite = selectedItems[i].icon;
                confirmSlots[i].color = Color.white;
            }
            else
            {
                confirmSlots[i].sprite = emptySlotSprite;
                confirmSlots[i].color = (emptySlotSprite != null) ? Color.white : new Color(1, 1, 1, 0);
            }
        }

        confirmModalRoot.SetActive(true);
    }

    void CloseConfirmModal()
    {
        isModalOpen = false;
        if (confirmModalRoot != null)
            confirmModalRoot.SetActive(false);
    }

    void StartGameFromModal()
    {
        if (ItemManager.Instance == null)
        {
            Debug.LogError("ItemManager.Instanceがいません");
            return;
        }

        ItemManager.Instance.runLoadout.Clear();

        var ids = selectedIds.ToList();
        foreach (var id in ids)
        {
            var item = ItemManager.Instance.ownedPermanent.FirstOrDefault(x => x != null && x.id == id);
            if (item != null)
            {
                ItemManager.Instance.ownedPermanent.Remove(item);
                ItemManager.Instance.runLoadout.Add(item);
            }
        }

        CloseConfirmModal();

        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.ResetTimer();
            GameTimer.Instance.StartTimer();
        }

        SceneManager.LoadScene("GameScene");
    }

    // =========================
    // 内部クラス：スロットの見た目制御
    // =========================
    class LoadoutSlotView
    {
        public Button button;
        Image icon;
        Image lockMask;
        GameObject frameObj;

        public LoadoutSlotView(GameObject root)
        {
            button = root.GetComponent<Button>();
            icon = root.transform.Find("Icon").GetComponent<Image>();
            lockMask = root.transform.Find("Lock").GetComponent<Image>();
            frameObj = root.transform.Find("Frame").gameObject;
        }

        public void SetIcon(Sprite s)
        {
            if (icon != null) icon.sprite = s;
        }

        public void SetLocked(bool locked)
        {
            if (lockMask != null) lockMask.gameObject.SetActive(locked);
            if (icon != null) icon.color = locked ? new Color(1f, 1f, 1f, 0.15f) : Color.white;
        }

        public void SetSelected(bool selected)
        {
            if (frameObj != null) frameObj.SetActive(selected);
        }

        public void SetInteractable(bool on)
        {
            if (button != null) button.interactable = on;
        }
    }
}
