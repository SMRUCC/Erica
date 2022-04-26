from setuptools import setup

setup(name='sakmapper',
      version='0.2',
      description='implementation of the mapper algorithm',
      url='https://github.com/szairis/sakmapper',
      author='sakellarios zairis',
      author_email='szairis@gmail.com',
      license='MIT',
      packages=['sakmapper'],
      install_requires=['numpy','scipy','pandas','scikit-learn','networkx'],
      zip_safe=False)
