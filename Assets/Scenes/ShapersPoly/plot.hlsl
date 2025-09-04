
void plot_float(float y, float pct , float size , out float Out){
    Out = smoothstep( pct - size,pct,y) - smoothstep(pct,pct + size,y);
}