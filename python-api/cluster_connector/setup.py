from setuptools import setup

setup(
    # Needed to silence warnings (and to be a worthwhile package)
    name='cluster-connector',
    url='https://github.com/heckej/P-O-Entrepreneurship-Team-A-ClusterConnector',
    author='Joren Van Hecke',
    author_email='joren.vanhecke@student.kuleuven.be',
    # Needed to actually package something
    packages=['cluster'],
    # Needed for dependencies
    install_requires=['websockets'],
    python_requires='>=3.7, <4',
    # *strongly* suggested for sharing
    version='1.1.4',
    # The license can be anything you like
    license='MIT',
    description='An API to communicate with the Cluster Connector server',
    # We will also need a readme eventually (there will be a warning)
    long_description=open('README.txt').read()
)
