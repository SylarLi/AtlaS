1.由于本工具采用的是替换UnityEngine.UI.dll和UnityEditor.UI.dll的机制，启用或禁用AtlaS支持后将会用Dll文件夹中的对应版本的dll替换Unity应用程序文件夹中的Dll，保险起见，在第一次使用本工具前，请先做好备份。具体影响的文件夹为Unity\Editor\Data\UnityExtensions\Unity\GUISystem。
2.点击菜单栏AtlaS/Switch/On(Off)启用或者关闭Atlas支持。
3.启用或者关闭Atlas支持之后，Unity将会自动关闭，需要手动重新打开Unity。
4.因为改写了Sprite（具体请参照UnityEngine.UI.Sprite）,游戏资源中的Sprite引用会进行一次迁移。目前支持的迁移类型为：
	A.附加在Prefab上的Image.cs/Selectable.cs/Dropdown.cs中的Sprite引用;
	B.附加在Scene上的Image.cs/Selectable.cs/Dropdown.cs中的Sprite引用;
	C.Animator/Animation上的AnimationClip中的Sprite帧引用（注：因为AnimationClip的限制，当启用AtlaS时，引用AnimationClip的GameObject会挂上一个ImageSpriteAnimationHook.cs脚本，用以替代正常的Sprite动画Key帧）；
如果需要支持迁移其他资源或脚本中的Sprite引用，请自行修改SpriteRefLog.cs。
