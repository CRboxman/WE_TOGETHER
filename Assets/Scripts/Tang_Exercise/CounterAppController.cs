using System.Collections;
using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CounterAppController : MonoBehaviour,IController
{
    // View
    private Button mBtnAdd;
    private Button mBtnSub;
    private TMP_Text mCountText;

    // 4. Model
    private CounterAppModel mModel;

    void Start()
    {
        // 5. 获取模型
        mModel = this.GetModel<CounterAppModel>();

        // View 组件获取
        mBtnAdd = transform.Find("BtnAdd").GetComponent<Button>();
        mBtnSub = transform.Find("BtnSub").GetComponent<Button>();
        mCountText = transform.Find("CountText").GetComponent<TMP_Text>();


        // 监听输入
        mBtnAdd.onClick.AddListener(() =>
        {
            // 6. 交互逻辑
            mModel.Count++;
            // 表现逻辑
            UpdateView();        
        });

        mBtnSub.onClick.AddListener(() =>
        {
            // 7. 交互逻辑
            mModel.Count--;
            // 表现逻辑
            UpdateView();
        });

        UpdateView();
    }

    void UpdateView()
    {
        mCountText.text = mModel.Count.ToString();
    }

    // 3.指定架构
    public IArchitecture GetArchitecture()
    {
        return CounterAppArc.Interface;
    }

    private void OnDestroy()
    {
        // 8. 将 Model 设置为空
        mModel = null;
    }
}

public class CounterAppModel : AbstractModel
{
    public int Count;
    protected override void OnInit()
    {
        Count = 0;
    }
}

public class CounterAppArc : Architecture<CounterAppArc>
{
    protected override void Init()
    {
        this.RegisterModel(new CounterAppModel());
    }
}
