all:
	g++ -O3 -fPIC -rdynamic -shared -D ARMA_DONT_USE_WRAPPER -o libArmaWrap.so -Wall -Werror -Wextra -pedantic -Wconversion -Wsign-conversion -msse2 -msse3 -msse4 -msse4.1 -msse4.2 -fopenmp -std=gnu++14 armawrap.cpp -lopenblas -larpack
