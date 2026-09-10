using System.Collections;
using System.Collections.Generic;
using QFramework;
using TMPro;
using TypingFXProFREE;
using UnityEngine;
using UnityEngine.UI;

public class CounterAppController : MonoBehaviour,IController
{
    // View
    private Button mBtnAdd;
    private Button mBtnSub;
    private TMP_Text mCountText;

    // 4. Model
    private ICounterAppModel mModel;

    void Start()
    {
        #region 获得注册的相关内容
        mModel = this.GetModel<ICounterAppModel>();
        var stotage = this.GetUtility<IStorage>();
        #endregion
        
        #region 表现逻辑相关对象获得
        // View 组件获取
        mBtnAdd = transform.Find("BtnAdd").GetComponent<Button>();
        mBtnSub = transform.Find("BtnSub").GetComponent<Button>();
        mCountText = transform.Find("CountText").GetComponent<TMP_Text>();
        #endregion

        #region 测试打印部分
        Debug.Log("当前初始保存的值："+this.SendQuery(new CountAllquery()));
        Debug.Log(stotage.LoadInt(nameof(mModel.Count)));
        Debug.Log((nameof(mModel.Count)));
        #endregion

        #region 将复用逻辑放入event容器中
        // this.RegisterEvent<CountAppChangeEvent>((e) =>
        // {
        //     UpdateView();        
        // }).UnRegisterWhenGameObjectDestroyed(gameObject);
        // this.RegisterEvent<CountAppChangeWithNumEvent>((num) =>
        // {
        //     print(num.Count+this.GetModel<CounterAppModel>().aCount.Value);
        // }).UnRegisterWhenGameObjectDestroyed(gameObject);
        mModel.Count.RegisterWithInitValue(count => UpdateView()).UnRegisterWhenGameObjectDestroyed(gameObject);
        
        #endregion
        
        #region 实际逻辑（外部的数据处理逻辑“命令”来减轻负担+本身这里的表现逻辑，因为控制器这里定义并存储着各个对象
        // 监听输入
        mBtnAdd.onClick.AddListener(() =>
        {
            // 6. 交互逻辑
            this.SendCommand<IncreaseCountCommand>();
        });
        mBtnSub.onClick.AddListener(() =>
        {
            // 7. 交互逻辑
            this.SendCommand<DecreaseCountCommand>();
            //print(this.SendCommand(new IncreaseCount_With_Num_Command(666)));
        });

        UpdateView();
        #endregion
        
    }
    private void UpdateView()
    {
        mCountText.text = mModel.Count.ToString();
    }
    
    #region 指定架构（获得里面注册的模型，可以访问数据了
    public IArchitecture GetArchitecture()
    {
        return CounterAppArc.Interface;
    }
    #endregion
    
    private void OnDestroy()
    {
        // 8. 将 Model 设置为空
        mModel = null;
    }
}

#region 模型（存共享数据，这里本身需要初始化

public interface ICounterAppModel : IModel
{
    BindableProperty<int> Count { get; }
}
public class CounterAppModel : AbstractModel,ICounterAppModel
{
    public BindableProperty<int> Count { get; } = new BindableProperty<int>();

    protected override void OnInit()
    {
        var storage = this.GetUtility<IStorage>();

        // 设置初始值（不触发事件）
        Count.SetValueWithoutEvent(storage.LoadInt(nameof(Count)));

        // 当数据变更时 存储数据
        Count.Register(newCount =>
        {
            storage.SaveInt(nameof(Count),newCount);
        });
    }
}
#endregion

#region 架构（注册模型，未来在控制器那里来指定，并访问模型数据
public class CounterAppArc : Architecture<CounterAppArc>
{
    protected override void Init()
    {
        this.RegisterModel<ICounterAppModel>(new CounterAppModel());
        
        this.RegisterUtility<IStorage>(new Storage());
        
        this.RegisterSystem<IAchievementSystem>(new AchievementSystem());
    }
}


#endregion

#region system管理长期检测的事件或者跨模块的内容
public interface IAchievementSystem:ISystem
{
    
}
public class AchievementSystem:AbstractSystem,IAchievementSystem
{
    protected override void OnInit()
    {
        var model=this.GetModel<ICounterAppModel>();
        // this.RegisterEvent<CountAppChangeEvent>(((e) =>
        // {
        //     if (model.aCount == 10)
        //     {
        //         Debug.Log("触发 点击达人 成就");
        //     }
        //     else if (model.aCount == 20)
        //     {
        //         Debug.Log("触发 点击专家 成就");
        //     } else if (model.aCount == -10)
        //     {
        //         Debug.Log("触发 点击菜鸟 成就");
        //     }
        // }));
        model.Count.RegisterWithInitValue(count =>
        {
            if (count == 10)
            {
                Debug.Log("触发 点击达人 成就");
            }
            else if (count == 20)
            {
                Debug.Log("触发 点击专家 成就");
            } else if (model.Count.Value == -10)
            {
                Debug.Log("触发 点击菜鸟 成就");
            }  
        });
    }
}


#endregion

#region 命令（一次性执行。作为外部处理数据用的部分，目前来说是为了分担控制器压力
public class IncreaseCountCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        #region 因为这里可以用GetModel，来快速访问所有模型，所以可以在这里随时处理模型数据
        this.GetModel<ICounterAppModel>().Count.Value += 1;
        #endregion

        #region 发送事件容器（一般装着复用重复逻辑，在这里接上上方的数据逻辑处理，具体实现还是在controller里，因为注册在那里
        this.SendEvent<CountAppChangeEvent>();
        //this.SendEvent(new CountAppChangeWithNumEvent{Count = 6});
        #endregion
    }
}

public class DecreaseCountCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<ICounterAppModel>().Count.Value -= 1;
        
        this.SendEvent<CountAppChangeEvent>();
    }
}

//public class IncreaseCountWithNumCommand : AbstractCommand<int>
// {
//     private int mNum = 1;
//     //有参构造函数，方便输入数据
//     public IncreaseCountWithNumCommand(int mNum)
//     {
//         this.mNum = mNum;
//     }
//     //有返回值的方法，需要和上方的泛型一致
//     protected override int OnExecute()
//     {
//         this.GetModel<CounterAppModel>().Count += mNum;
//         return mNum;
//     }
//     
// }
#endregion

#region Event容器（事件容器，存放复用逻辑，从controller中抽取出来的
/// <summary>
/// 事件容器（ui更新
/// </summary>
public struct CountAppChangeEvent
{
    
}
/// <summary>
/// 事件容器（含参int
/// </summary>
public struct CountAppChangeWithNumEvent
{
    public int Count;
}
#endregion

#region Utility（提取公共方法，方便各处获得，当做第三方的那种不是静态的工具类
public interface IStorage : IUtility
{
    void SaveInt(string key, int value);
    int LoadInt(string key, int defaultValue = 0);
}
public class Storage : IStorage
{
    public void SaveInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
    }

    public int LoadInt(string key, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(key, defaultValue);
    }
}

#endregion

#region query专门用来查询数据或者计算的类

public interface IMyQuery:IQuery<int>{

}
public class CountAllquery : AbstractQuery<int>,IMyQuery
{
    protected override int OnDo()
    {
        return this.GetModel<ICounterAppModel>().Count.Value;
    }
}
#endregion



