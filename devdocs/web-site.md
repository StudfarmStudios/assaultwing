# Assault Wing web site development notes

Web content is in the main repository under `/docs`. It is pure
static HTML extracted from an earlier incarnation of `assaultwing.com`.

The GitHub URL for the web pages is:
https://studfarmstudios.github.io/assaultwing/

The web site is hosted with GitHub Pages.  There is a separate branch
`web-publish` for, but it should track `master`. The separate branch exists to
control when web changes are published. Web development should happen in the
normal way through PRs to master and when they need to be published a fast
forward merge from `master` -> `web-publish` is done to publish them.

## Web development TODOs:

- Create instruction and other information pages based on the
  information in the
- Bunch of other touch ups and improvements...
- Consider deploying a static site generator. GitHub recommends
  `Jekyll`.
- Point the assaultwing.com domain to the GitHub pages
- SSL certificate for assaultwing.com. GitHub seems to support this,
  but details need to be checked.