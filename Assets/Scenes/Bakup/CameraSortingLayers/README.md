# URP 使用 CameraSortingLayer 处理限定区域特效


## 简介

CameraSortingLayerTexture 是 URP 中用于2D渲染的特殊纹理，主要用于自定义着色器中获取特定排序层的渲染结果。类似于渲染管线中的GrabPass，但针对 2D 渲染进行了优化和适配。


## 工作原理

- CameraSortingLayerTexture 本质是一个渲染纹理，用于捕获从后到前直至指定排序层级的所有渲染内容
- 纹理数据由 `2D Render Data Asset` 配置和生成，可以在自定义着色器中通过 ``_CameraSortingLayerTexture`` 变量访问
- 适用于创建各种2D特效，如热雾效果、折射水面、局部模糊等覆盖效果

## 关键配置

1. 2D Render Data 设置
   - 在 2D Render Data Asset中可以配置 CameraSortingLayerTexture的行为
   - Foremost Sorting Layer： 指定捕获范围，从最后层到该指定层的所有内容都将被捕获
   - Downsampling Method：提供4种降采样方法，用于控制纹理分辨率
2. 着色器设置
   - 在Shader Graph 中声明 Texture 2D 属性，引用名为 `_CameraSortingLayerTexture`
   - 确保取消勾选 "Exposed" 选项，使其能够获取全局纹理
   - 使用该纹理的精灵，必须设置在比捕获图层更高的排序层，避免获取上一帧数据导致视觉异常。

## 使用场景与限制

### 适用场景

- 2D 游戏中的局部特效渲染
- 需要基于已有渲染内容的后处理效果
- 模拟透明材质的折射和反射效果

### 已知限制

- 不包含光照信息，捕获的是未经过光照处理的原始渲染结果
- 场景视图相机可能会影响纹理捕获结果，增加调试难度
- 多相机设置下可能出现预期外的行为
- 与 3D 渲染功能存在兼容性限制


## 最佳实践

1. 图层管理
   - 创建专用的排序层结构：捕获层 → 使用层的层次结构
   - 确保使用纹理的对象位于捕获范围之外的前序排序层
2. 性能优化
   - 根据目标平台选择合适的降采样方法
   - 移动平台可考虑禁用深度 / 模板缓冲区以提升性能（如果不使用 Sprite Mask 等功能）
3. 调试技巧
   - 参考 Unity 官方示例项目中的 "Heat Haze Overlay" 场景
   - 开发时暂时禁用场景视图以避免干扰捕获结果



## 示范步骤
1. 创建精灵着色器
2. 上下左右偏移乘以偏移系数，叠加原图获得叠加的颜色和透明度，除以5
3. 创建屏幕渲染纹理，使用屏幕位置作为uv映射
4. 使用 CameraSortingLayerTexture 作为 Texture2D 名称 （属性名必须是：_CameraSortingLayerTexture）
5. 新增 Sprite Sorting Layer ： CameraSortingLayer
6. 在 Renderer 2D Data -> Camera Sorting Layer Texture -> foremost Sorting Layer 下设置捕获 CameraSortingLayer 上一层的图层

> 关键点： CameraSortingLayerTexture  的 expose 得取消勾选，才可以捕获全局的 CameraSortingLayerTexture



## 原理

- CameraSortingLayerTexture 是一个渲染纹理


## 实现示范
[基于CameraSortingLayerTexture的模糊纹理效果,urp 14.0.2](./ShockWaveSprite.shadergraph)

## 参考

[限定涂层模糊](https://www.youtube.com/watch?v=8-E8Vp0l6wg&list=PLfmYNuLHEy-Mj5tY2C3PAo3nJx2U19FKb&index=5)