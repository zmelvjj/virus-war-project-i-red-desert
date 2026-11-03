#include <stdio.h>

enum Color { a, b, c };
enum Orange { Org_R = 255, Org_G = 165, Org_B = 0 };


int main() {

    enum Color color = a;

    color = (enum Color)Org_R;


    printf("%d\n", color);

    return 0;
}