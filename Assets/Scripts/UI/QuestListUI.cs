using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class QuestListUI : MonoBehaviour
{
    public MenuNavigation menuNavigation;
    public Button QuestButton;
    [SerializeField] private GameObject questItemPrefab;

    private GameObject firstItem;
    private GameObject secondItem;

    private void OnEnable()
    {
        firstItem = null;
        RefrescarLista();
    }

    private void RefrescarLista()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        
        int index = 0;
        
        foreach (var quest in QuestManager.instance.GetActiveQuests())
        {
            GameObject item = Instantiate(questItemPrefab, transform);
            item.GetComponent<QuestItemUI>().Setup(quest);

            if (index == 0)
            {
                firstItem = item;
            }
            
            else if (index == 1)
            {
                secondItem = item;
            }
            
            index++;
            
        }

        if (firstItem != null)
        {
            RefreshNavigation(firstItem.GetComponent<Selectable>());   
        }
    }

    private void RefreshNavigation(Selectable selectable)
    {
        menuNavigation.navigationButtons[2].DownButtonNav = selectable;
        
        Navigation nav = selectable.navigation;
        nav.mode = Navigation.Mode.Explicit;
        nav.selectOnUp = QuestButton;
        if (secondItem != null)
        {
            nav.selectOnDown = secondItem.GetComponent<Selectable>();
        }
        selectable.navigation = nav;
        
        menuNavigation.UpdateNavigation(QuestButton);
    }
}
