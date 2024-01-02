/*
 * QueryPage.
 */
const queryPage = {
    template: document.querySelector('#query-page'),
    data() {
        return {
            query: Vue.ref(''),
            rows: [],
            filter: Vue.ref(''),
            columns: [],
            schema: []
        }
    },
    methods: {
        runQuery: function(evt) {
            const self = this;
            axios({
                method: 'post',
                url: window.baseUrl + 'api/query',
                data: {
                    query: self.query
                }
            })
                .then(function(response) {
                    self.columns = response.data.schema.map(function(col) {
                        return {
                            label: col.name,
                            name: col.name,
                            field: col.name,
                            align: 'left',
                            sortable: true,
                            headerStyle: 'font-weight: bold'
                        }
                    });
                    self.rows = response.data.data;
                    localStorage.setItem('lastQuery', self.query);
                });
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
        this.query = localStorage.getItem('lastQuery');
        if (location.protocol !== 'https:') {
            Quasar.Notify.create({
                type: 'warning',
                message: 'You are using insecured HTTP connection!'
            });
        }
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
        getFullPath: function(name) {
            if (this.pathElements.length === 0) {
                this.pathElements.push('/');
            }
            var fullPath = name !== '../' ? this.pathElements.join('/')
                : this.pathElements.slice(0, -1).join('/') + '/';
            if (name.length > 0 && name !== '../') {
                fullPath += '/' + name;
            }
            return fullPath;
        },
        runQuery: function() {
            const self = this;
            const path = self.$route.query.q;
            // Download file.
            if (path && path.length > 0 && path[path.length - 1] !== '/') {
                self.$router.go(-1);
                window.location.assign(window.baseUrl + 'api/files?q=' + path);
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
            const elements = to.split('/');
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
        }
    },
    mounted: function() {
        this.setPathElements(this.$route.query.q);
        this.runQuery();
    }
}
