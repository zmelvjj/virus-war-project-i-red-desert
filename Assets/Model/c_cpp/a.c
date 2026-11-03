#include <stdio.h>

void a(int arr[]) {
    arr[0] = 10;
}

int main() {
    int as[] =  {1,2,3,4,5};

    a(as);

    printf("%d\n", as[0]);
}