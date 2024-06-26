/*
 * QueryPage.
 */
const queryPage = {
    template: document.querySelector('#query-page'),
    data: function() {
        return {
            query: Vue.ref(''),
            rows: [],
            filter: Vue.ref(''),
            columns: [],
            schema: []
        }
    },
    methods: {
        renderResult: function(result) {
            const self = this;
            self.columns = result.schema.map(function(col) {
                return {
                    label: col.name,
                    name: col.name,
                    field: col.name,
                    align: 'left',
                    sortable: true,
                    headerStyle: 'font-weight: bold'
                }
            });
            self.rows = result.data;
            localStorage.setItem('lastQuery', self.query);
        },
        runSchema: function() {
            const self = this;
            axios({
                method: 'post',
                url: window.baseUrl + 'api/schema',
                data: {
                    query: self.query
                }
            }).then(function(response) {
                self.renderResult(response.data);
            });
        },
        runQuery: function(params) {
            const self = this;
            axios({
                method: 'post',
                url: window.baseUrl + 'api/query',
                data: {
                    query: self.query,
                    parameters: params
                }
            }).then(function(response) {
                self.renderResult(response.data);
            });
        },
        reset: function() {
            this.query = '';
            this.rows = [];
            this.filter = '';
            this.columns = [];
            this.schema = [];
        }
    },
    setup: function() {
        return {
            pagination: Vue.ref({
                rowsPerPage: 0
            }),
            splitterModel: Vue.ref(40)
        }
    },
    mounted: function() {
        const self = this;
        self.query = localStorage.getItem('lastQuery');
        if (location.protocol !== 'https:') {
            Quasar.Notify.create({
                type: 'warning',
                message: 'You are using insecure HTTP connection!'
            });
        }
    },
    beforeRouteEnter: function(to, from, next) {
        const globalProperties = window.app.config.globalProperties;
        next(function(vm){
            if (!globalProperties.path) {
                return;
            }
            setTimeout(function() {
                vm.reset();
                vm.query = "SELECT TOP 200 * FROM '" + globalProperties.path.replaceAll("'", "''") + "';";
                vm.runQuery([{
                    key: '_path',
                    value: 's:' + globalProperties.path
                }]);
                globalProperties.path = '';
            });
        });
    }
}

/*
 * InfoPage.
 */
const infoPage = {
    template: document.querySelector('#info-page'),
    data: function() {
        return {
            version: '',
            os: '',
            installedPlugins: '',
            platform: ''
        }
    },
    mounted: function() {
        const self = this;
        axios({
            url: window.baseUrl + 'api/info'
        })
        .then(function(response) {
            self.version = response.data.version;
            self.os = response.data.os;
            self.installedPlugins = response.data.installedPlugins;
            self.platform = response.data.platform;
        });
    }
}

/*
 * Files page.
 */
const filesPage = {
    template: document.querySelector('#files-page'),
    data: function() {
        return {
            pathElements: ['/'],
            rows: [],
            columns: [{
                name: 'name',
                label: 'Name',
                field: 'name',
                align: 'left',
                headerStyle: 'font-weight: bold',
                sortable: true
            }, {
                name: 'last_write_time',
                label: 'Last Update',
                field: 'last_write_time',
                align: 'left',
                headerStyle: 'font-weight: bold',
                sortable: false
            }, {
                name: 'size',
                label: 'Size',
                field: 'size',
                align: 'left',
                headerStyle: 'font-weight: bold',
                sortable: true
            }, {
                name: 'actions',
                label: 'Actions',
                align: 'left',
                headerStyle: 'font-weight: bold',
                sortable: false
            }]
        }
    },
    watch: {
        '$route.query.q': function(to, from) {
            this.setPathElements(to);
            this.runQuery();
        }
    },
    methods: {
        getFullPath: function(name, index) {
            var fullPath = '/';
            fullPath += name !== '../' ? this.pathElements.slice(1, index).join('/')
                : this.pathElements.slice(1, -1).join('/') + '/';
            if (name.length > 0 && name !== '../') {
                if (!fullPath.endsWith('/')) {
                    fullPath += '/';
                }
                fullPath += name;
            }
            return fullPath;
        },
        getFullDownloadPath: function(path) {
            return window.baseUrl + 'api/files?q=' + path;
        },
        getFileExtension: function(filename) {
            return filename.split('.').pop().toLowerCase();
        },
        getFileIcon: function(name) {
            const extension = this.getFileExtension(name);
            switch (extension) {
                case 'txt':
                case 'log':
                case 'md':
                    return 'text_snippet';
                case 'pdf':
                    return 'picture_as_pdf';
                case 'avi':
                case 'asf':
                case 'asx':
                case 'flv':
                case 'mkv':
                case 'mov':
                case 'mp4':
                case 'mp4v':
                case 'mpeg':
                case 'mpg':
                case 'oga':
                case 'ogg':
                case 'ogv':
                case 'ts':
                case 'webm':
                case 'wmv':
                    return 'videocam';
                case 'aac':
                case 'm3u':
                case 'm4a':
                case 'mp3':
                case 'wav':
                    return 'audiotrack';
                case '7z':
                case 'bz':
                case 'bz2':
                case 'gz':
                case 'zip':
                case 'rar':
                    return 'folder_zip';
                case 'bpm':
                case 'gif':
                case 'ico':
                case 'jfif':
                case 'jpeg':
                case 'jpg':
                case 'png':
                case 'svg':
                case 'tif':
                case 'tiff':
                case 'wbmp':
                case 'webp':
                    return 'image';
                case 'js':
                case 'json':
                    return 'javascript';
                case 'css':
                    return 'css';
                case 'htm':
                case 'html':
                case 'shtml':
                    return 'html';
                case 'ical':
                case 'icalendar':
                    return 'event';
                case 'rss':
                    return 'rss_feed';
                case 'eml':
                    return 'email';
                default:
                    return 'description';
            }
        },
        runQuery: function() {
            const self = this;
            const path = self.$route.query.q;
            // Download file.
            if (path && path.length > 0 && path[path.length - 1] !== '/') {
                self.$router.go(-1);
                window.location.assign(self.getFullDownloadPath(path));
                return;
            }
            axios({
                url: window.baseUrl + 'api/files',
                params: {
                    q: path
                }
            })
            .then(function(response) {
                self.rows = response.data.data;
            });
        },
        setPathElements: function(to) {
            to = to ?? '';
            const elements = ['/'].concat(to.split('/'));
            // Convert /home/user/temp/.. to /home/user/.
            for (var i = 1; i < elements.length; i++) {
                if (elements[i] === '..') {
                    elements[i] = '';
                    elements[i - 1] = '';
                }
            }
            this.pathElements = elements.filter(function (el) {
                return el.length > 0;
            });
        },
        setPathQuery: function(path) {
            app.config.globalProperties.path = path;
        }
    },
    mounted: function() {
        this.setPathElements(this.$route.query.q);
        this.runQuery();
    }
}
