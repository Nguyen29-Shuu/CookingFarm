using UnityEngine;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance;

    // Dùng typed component reference để gọi IsOpen thật sự của từng popup,
    // tránh lỗi khi parent container luôn activeInHierarchy.
    [Header("Block Click Popups")]
    [SerializeField] private WarehousePopupUI  warehousePopup;
    [SerializeField] private PigPenPopupUI     pigPenPopup;
    [SerializeField] private ChickenPenPopupUI chickenPenPopup;
    [SerializeField] private CowPenPopupUI     cowPenPopup;
    [SerializeField] private MarketPopupUI     marketPopup;
    [SerializeField] private TrainProcessPopupUI trainProcessPopup;
    [SerializeField] private TrainLoadPopupUI   trainLoadPopup;

    private void Awake()
    {
        Instance = this;
        Debug.Log("[PopupManager] Awake");
    }

    public bool IsAnyPopupOpen()
    {
        return (warehousePopup    != null && warehousePopup.IsOpen)
            || (pigPenPopup       != null && pigPenPopup.IsOpen)
            || (chickenPenPopup   != null && chickenPenPopup.IsOpen)
            || (cowPenPopup       != null && cowPenPopup.IsOpen)
            || (marketPopup       != null && marketPopup.IsOpen)
            || (trainProcessPopup != null && trainProcessPopup.IsOpen)
            || (trainLoadPopup    != null && trainLoadPopup.IsOpen);
    }
}
