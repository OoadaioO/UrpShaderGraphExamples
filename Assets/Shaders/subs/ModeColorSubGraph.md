## 动态亮度调整

场景：

可以调整整体亮度


```c
float4 ColorMod(float4 c, float d) {
    return c - (c - c * c) * (d - 1);
}
```


- 0 < d < 1
  - 随着 c 值变大逐渐变大
- d > 1
  - 随着 c 值变大逐渐变小
- d = 1
  - 原始值

图像示意：

https://www.desmos.com/calculator/ebdyflt2v7