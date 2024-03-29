tmpinst = _tmpinst

noinst_SCRIPTS = mono-wrapper monodis-wrapper

etctmp = etc
symlinks = etc/mono/1.0/machine.config etc/mono/2.0/machine.config etc/mono/2.0/web.config etc/mono/browscap.ini etc/mono/2.0/Browsers/Compat.browser

if INSTALL_4_x
symlinks += etc/mono/4.0/machine.config etc/mono/4.0/web.config etc/mono/4.5/web.config etc/mono/4.5/machine.config etc/mono/4.0/Browsers/Compat.browser etc/mono/4.5/Browsers/Compat.browser
endif

etc/mono/2.0/machine.config: $(top_srcdir)/data/net_2_0/machine.config
etc/mono/2.0/web.config: $(top_srcdir)/data/net_2_0/web.config
etc/mono/browscap.ini: $(top_srcdir)/data/browscap.ini
etc/mono/2.0/Browsers/Compat.browser: $(top_srcdir)/data/Browsers/Compat.browser
etc/mono/4.0/Browsers/Compat.browser: $(top_srcdir)/data/Browsers/Compat.browser
etc/mono/4.5/Browsers/Compat.browser: $(top_srcdir)/data/Browsers/Compat.browser
etc/mono/4.0/machine.config: $(top_srcdir)/data/net_4_0/machine.config
etc/mono/4.0/web.config: $(top_srcdir)/data/net_4_0/web.config
etc/mono/4.5/machine.config: $(top_srcdir)/data/net_4_5/machine.config
etc/mono/4.5/web.config: $(top_srcdir)/data/net_4_5/web.config

$(symlinks):
	cd $(top_builddir) && $(SHELL) ./config.status $(subdir)/$@

SUPPORT_FILES = $(symlinks) mono-wrapper etc/mono/config

build_profiles = 

if INSTALL_4_x
build_profiles += binary_reference_assemblies net_4_x xbuild_12 xbuild_14
al_profile = net_4_x
endif

if INSTALL_MONODROID
build_profiles += monodroid
endif

if INSTALL_MONOTOUCH
build_profiles += monotouch monotouch_runtime
endif

if INSTALL_MONOTOUCH_WATCH
build_profiles += monotouch_watch monotouch_watch_runtime
endif

if INSTALL_MONOTOUCH_TV
build_profiles += monotouch_tv monotouch_tv_runtime
endif

if INSTALL_MOBILE_STATIC
build_profiles += mobile_static
endif

if INSTALL_XAMMAC
build_profiles += xammac xammac_net_4_5
endif

test_profiles = $(build_profiles)

if BUILD_MCS

MAKE_FLAGS=$(if $(V),,--no-print-directory -s)

# The write check is to foil 'make distcheck'
all-local: $(SUPPORT_FILES) $(TEST_SUPPORT_FILES)
	if test -w $(mcs_topdir); then :; else chmod -R +w $(mcs_topdir); fi
	cd $(mcs_topdir) && $(MAKE) $(MAKE_FLAGS) NO_DIR_CHECK=1 PROFILES='$(build_profiles)' CC='$(CC)' all-profiles

# override automake
install: install-exec install-data

# override automake
install-exec: $(SUPPORT_FILES) $(TEST_SUPPORT_FILES)
	cd $(mcs_topdir) && $(MAKE) NO_DIR_CHECK=1 PROFILES='$(build_profiles)' RUNTIME_HAS_CONSISTENT_GACDIR=yes prefix=$(prefix) install-profiles

# override automake
install-data:
	@:

# override automake
uninstall:
	cd $(mcs_topdir) && $(MAKE) NO_DIR_CHECK=1 PROFILES='$(build_profiles)' RUNTIME_HAS_CONSISTENT_GACDIR=yes prefix=$(prefix) uninstall-profiles

clean-local:
	cd $(mcs_topdir) && $(MAKE) NO_DIR_CHECK=1 PROFILES='$(build_profiles)' clean-profiles
	-rm -fr $(etctmp) $(tmpinst)

endif BUILD_MCS

TEST_SUPPORT_FILES = $(tmpinst)/bin/mono $(tmpinst)/bin/ilasm $(tmpinst)/bin/mcs $(tmpinst)/bin/al

mcs-do-test-profiles:
	cd $(mcs_topdir) && $(MAKE) NO_DIR_CHECK=1 PROFILES='$(test_profiles)' test-profiles

mcs-do-run-test-profiles: test-support-files
	cd $(mcs_topdir) && $(MAKE) NO_DIR_CHECK=1 PROFILES='$(test_profiles)' run-test-profiles

if HOST_WIN32
if CROSS_COMPILING
cur_dir_cmd = pwd
PLATFORM_PATH_SEPARATOR = :
else
cur_dir_cmd = cygpath -w -a .
PLATFORM_PATH_SEPARATOR = ;
endif
else
cur_dir_cmd = pwd
PLATFORM_PATH_SEPARATOR = :
endif

if INSTALL_MOBILE_STATIC
#  ILASM.exe has features which a mobile_static runtime will not support.
#  It is invoked with an external mono when used in the runtime.
#  We skip it here because otherwise it will fail to verify.
MOBILE_STATIC_FILTER=grep -v ilasm
else
MOBILE_STATIC_FILTER=echo
endif

# Compile all assemblies with the verifier turned on. Code must be valid but not verifiable.
# TODO it would be nice to split assemblies without unsafe code to use the verifier with verifiable mode.
# Skip binary_reference_assemblies because they contain metadata only
mcs-compileall: mono-wrapper etc/mono/config
	export verifiable_files=`ls "$(mcs_topdir)/class/lib/$$profile/" | grep -E '\.(dll|exe)$$' | $(MOBILE_STATIC_FILTER)` ; \
	save_MONO_PATH=$$MONO_PATH; mcs_topdir=`cd $(mcs_topdir) && $(cur_dir_cmd)`; ok=:; \
	for profile in $(test_profiles); do \
	  if [ "binary_reference_assemblies" = "$$profile" ]; then \
          continue; \
      fi; \
	  if [ "xbuild_12" = "$$profile" ]; then \
		  MONO_PATH="$$mcs_topdir/class/lib/$$profile$(PLATFORM_PATH_SEPARATOR)$$mcs_topdir/class/lib/net_4_x$(PLATFORM_PATH_SEPARATOR)$$save_MONO_PATH"; \
	  elif [ "xbuild_14" = "$$profile" ]; then \
		  MONO_PATH="$$mcs_topdir/class/lib/$$profile$(PLATFORM_PATH_SEPARATOR)$$mcs_topdir/class/lib/net_4_x$(PLATFORM_PATH_SEPARATOR)$$save_MONO_PATH"; \
	  else \
		  MONO_PATH="$$mcs_topdir/class/lib/$$profile$(PLATFORM_PATH_SEPARATOR)$$save_MONO_PATH"; \
	  fi; \
	  export MONO_PATH; \
	  for stub in $$verifiable_files; do \
	  i=$(mcs_topdir)/class/lib/$$profile/$$stub ; \
	  echo $$i ; \
		if [ ! -f $$i ] ; then \
			continue ; \
		fi ;  \
	    if ./mono-wrapper --compile-all --verify-all --security=verifiable $$i; then \
	      echo $$i verified OK; \
	    else \
	      echo $$i verification failed; ok=false; \
	    fi; done; done; \
	$$ok

if NACL_CODEGEN
check-local:
else
check-local: mcs-compileall mcs-do-test-profiles
	$(MAKE) mcs-do-run-test-profiles
endif

# Compile all mcs tests
test: mcs-do-test-profiles

CLEANFILES = etc/mono/config

# depend on $(symlinks) to ensure 'etc/mono' directory exists
etc/mono/config: ../data/config Makefile $(symlinks)
	d=`cd ../support && pwd`; \
	sed 's,target="$$mono_libdir/libMonoPosixHelper$(libsuffix)",target="'$$d'/libMonoPosixHelper.la",' ../data/config > $@t
	if test -z "$(libgdiplus_loc)"; then :; else \
	  sed 's,target="$(libgdiplus_install_loc)",target="$(libgdiplus_loc)",' $@t > $@tt; \
	  mv -f $@tt $@t; fi
	mv -f $@t $@

$(tmpinst)/bin/mono: mono-wrapper etc/mono/config
	$(mkinstalldirs) $(tmpinst)/bin
	cp mono-wrapper $@

$(tmpinst)/bin/mcs: $(tmpinst)/bin/mono Makefile
	echo '#! /data/data/com.termux/files/usr/bin/bash' > $@ ; \
	r=`pwd`; m=`cd $(mcs_topdir) && pwd`; \
	echo 'exec "'"$$r/$(tmpinst)/bin/mono"'" "'"$$m/class/lib/build/mcs.exe"'" "$$@"' >> $@ ; \
	chmod +x $@

$(tmpinst)/bin/ilasm: $(tmpinst)/bin/mono Makefile
	echo '#! /data/data/com.termux/files/usr/bin/bash' > $@ ; \
	r=`pwd`; m=`cd $(mcs_topdir) && pwd`; \
	echo 'exec "'"$$r/$(tmpinst)/bin/mono"'" "'"$$m/ilasm/ilasm.exe"'" "$$@"' >> $@ ; \
	chmod +x $@

$(tmpinst)/bin/al: $(tmpinst)/bin/mono Makefile
	echo '#! /data/data/com.termux/files/usr/bin/bash' > $@ ; \
	r=`pwd`; m=`cd $(mcs_topdir) && pwd`; \
	echo 'exec "'"$$r/$(tmpinst)/bin/mono"'" "'"$$m/class/lib/$(al_profile)/al.exe"'" "$$@"' >> $@ ; \
	chmod +x $@

test-support-files: $(TEST_SUPPORT_FILES)
	@:

# the 'cygnus' option also disables the default 'distdir:' target, which we _do_ want
MYDISTFILES = $(DIST_COMMON)
distdir: $(MYDISTFILES)
	rm -fr $(distdir)
	mkdir $(distdir)
	test -z '$(MYDISTFILES)' || for file in ''$(MYDISTFILES); do \
	  cp -p $$file $(distdir) ; done
	find $(distdir) -type f -exec chmod a+r {} ';'
