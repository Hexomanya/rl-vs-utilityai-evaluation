using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSkills.Scripts.Ui
{
    public class SkillDisplayController : DisplayController
    {
        private const float ShowSkillTimeSeconds = 2f;
        
        [SerializeField] private GameObject _rootObject;
        [SerializeField] private Image _skillDisplay;
        
        public override void SetHidden(bool isHidden)
        {
            //_rootObject.SetActive(!isHidden);   
            _rootObject.SetActive(false); 
        }

        public void UpdateSkill(SimpleSkill lastUsedSkill)
        {
            _skillDisplay.sprite = lastUsedSkill.Icon;
            this.StartCoroutine(this.ShowSkill());
        }

        private IEnumerator ShowSkill()
        {
            this.SetHidden(false);
            yield return new WaitForSeconds(ShowSkillTimeSeconds);
            this.SetHidden(true);
        } 
    }
}
