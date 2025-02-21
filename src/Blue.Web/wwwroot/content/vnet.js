const uintMax = 4294967295;

class IpRange {
    
    constructor(address, cidrBits) {
        this.address = address;
        this.cidrBits = cidrBits;

        this._min = address;
        
        let hostBits = 32 - cidrBits;
        this._max = address + Math.pow(2, hostBits);
    }

    /**
     * 
     * @param range {IpRange}
     */
    equals(range) {
        return this.min === range.min &&
            this.max === range.max;
    }

    /**
     * 
     * @param range {IpRange}
     */
    contains(range) {
        return range.min >= this.min && range.max <= this.max;
    }
    
    /**
     * @returns {number}
     */
    get address() {
        return this._address;
    }

    set address(value) {
        this._address = value;
    }

    /**
     * @returns {number}
     */
    get cidrBits(){
        return this._cidrBits;
    }
    
    get min(){
        return this._min;
    }
    
    get max() {
        return this._max;
    }
    
    get size() {
        return this.max - this.min;
    }

    set cidrBits(value) {
        this._cidrBits = value;
    }
    
    static parse(value) {
        let octets = [0, 0, 0, 0];
        let builder = "";

        let octetIndex = 0;
        let cidrIndex = 0;

        for (let ii=0; ii<value.length; ii++) {
            const c = value[ii];

            cidrIndex++;
            switch (c) {
                case '.':
                    if (octetIndex >= 3) {
                        return undefined;
                        //throw `Invalid CIDR '${value}'. Four octets is enough at ${cidrIndex}`;
                    }
                    octets[octetIndex] = parseInt(builder);
                    builder = "";
                    octetIndex++;
                    break;
                case '/':
                    if (octetIndex < 3) {
                        return undefined;
                        //throw `Invalid CIDR '${value}'. Expected more numbers at ${cidrIndex}`;
                    }

                    octets[octetIndex] = parseInt(builder);
                    builder = "";
                    octetIndex++;
                    break;
                default:
                    builder += c;
                    break;
            }
        }

        if (octetIndex !== 4 && builder.length > 0)
        {
            octets[octetIndex] = parseInt(builder);
            builder = "";
            octetIndex++;
        }

        if (octetIndex !== 4)
        {
            return undefined;
            //throw `Invalid CIDR '${value}'. Expected more numbers at ${cidrIndex}`;
        }
        const address = ((octets[0] * (1 << 24)) >>> 0) +
            ((octets[1] * (1 << 16)) >>> 0) +
            ((octets[2] * (1 << 8)) >>> 0) +
            (octets[3] >>> 0);
        return new IpRange(address, builder.length > 0 ? parseInt(builder) : 255);
    }
    
    split() {
        return this.splitInto(2);
    }
    
    splitInto(number) {
        if (number % 2 !== 0) {
            throw `Cannot split into ${number}. Must be positive and dividable by 2`;
        }

        const cidrBits = this._cidrBits + Math.log2(number);

        if (cidrBits > 32) {
            return undefined;
        }
        
        const chunk = this.size / number;
        let subRanges = [];
        for (let ii=0; ii< number; ii++) {
            subRanges.push(new IpRange(this.min + ii * chunk, cidrBits));
        }
        return subRanges;
    }

    get parent() {
        if (this._cidrBits > 0) {
           const cidrBits = this._cidrBits-1;
           
           let min = this.min;
           for (let ii=31-cidrBits; ii>=0; ii--){
               min &= ~1<<ii;
           }
           
           return new IpRange(min, cidrBits);
        }
        if (this.min > 0) {
            return new IpRange(this.min / 2, this._cidrBits);
        }
        return undefined;
    }

    get value() {
        return this.format();
    }
    
    format() {
        const address = this.address;
        const octets = [
            (address >> 24) & 0xff,
            (address >> 16) & 0xff,
            (address >> 8) & 0xff,
            (address & 0xff)
        ];
        
        const ip = octets.join('.');
        return `${ip}/${this.cidrBits}`;
    }
    
    info() {
        return `${this.format()} (${this.size})`
    }
}

class VNetElement extends HTMLElement {

    static observedAttributes = ["cidr"];

    constructor() {
        super();
        this._left = undefined;
        this._right = undefined;
        this.addEventListener("mouseover", this.hover.bind(this));
        this.addEventListener("mouseout", this.unhover.bind(this));
    }
    
    hover(e) {
        this.classList.add("hovered");
        if (this._entries && this._entries.length > 0) {
            for (let ii=0; ii<this._entries.length; ii++) {
                this.model.highlightEntry(this._entries[ii]);
            }
        }
    }
    
    unhover(e) {
        this.classList.remove("hovered");
        if (this._entries && this._entries.length > 0) {
            for (let ii=0; ii<this._entries.length; ii++) {
                this.model.unhighlightEntry(this._entries[ii]);
            }
        }
    }

    /**
     * 
     * @param e {VNetEntry}
     */
    highlight(e) {
        if (e.ranges.some(r => r.equals(this.range))){
            this.classList.add("highlighted");
        }
    }
    
    unhighlight(e) {
        if (e.ranges.some(r => r.equals(this.range))){
            this.classList.remove("highlighted");
        }
    }
    /**
     * @returns {string}
     */
    get direction() {
        const parent = this.parent;
        if (!parent) {
            return "horizontal";
        }
        const parentValue = parent.direction;
        switch(parentValue) {
            case "vertical":
                return "horizontal";
            case "horizontal":
                return "vertical";
            default:
                return "horizontal";
        }
    }

    get parent() {
        return this._parent;
    }

    set parent(parent) {
        this._parent = parent;
    }

    /**
     * @returns {VNetElement | undefined}
     */
    get left(){
        return this.left;
    }
    set left(value) {
        value.parent = this;
        value.model = this.model;
        value.range = this.range.split()[1];
        this._left = value;
        this.appendChild(value);
    }

    /**
     * @returns {VNetElement | undefined}
     */
    get right(){
        return this._right;
    }
    
    set right(value) {
        value.parent = this;
        value.model = this.model;
        value.range = this.range.split()[0];
        this._right = value;
        this.appendChild(value);
    }

    split() {
        if (this._left || this._right) {
            return;
        }

        const subs = this.range.split();
        this._left = new VNetElement();
        this._left.parent = this;
        this._left.range = subs[0];
        this._left.model = this.model;
        this._container.appendChild(this._left);
        
        
        this._right = new VNetElement();
        this._right.parent = this;
        this._right.range = subs[1];
        this._right.model = this.model;
        this._container.appendChild(this._right);
        
    }

    /**
     * @param removeFromModel {Boolean}
     */
    removeChildren(removeFromModel) {
        if (this._left) {
            this._left.remove(removeFromModel);
            this._left = undefined;
        }
        if (this._right) {
            this._right.remove(removeFromModel);
            this._right = undefined;
        }
    
        if (this._container){
            this._container.innerHTML = "";    
        }
    }

    /**
     * 
     * @returns {IpRange}
     */
    get range(){
        return this._range;
    }
    
    set range(value) {
        this._range = value;
    }

    /**
     * 
     * @returns {string | undefined}
     */
    get name() {
        return this._name;
    }
    
    set name(value) {
        if (!value || value.match(/^ *$/) !== null) {
            this.model.remove(this._name, this.range);
            this.classList.remove("highlighted");
            this._name = undefined;
        }
        else {
            this.model.add(value, this.range.format());
            this._name = value;
        }
    }
    
    /**
     * @returns {VNetModel}
     */
    get model() {
        return this._model;
    }
    
    set model(value) {
        this.unregister(this.model);
        this._model = value;
        value.addEventListener("add", this.render.bind(this));
        value.addEventListener("remove", this.render.bind(this));
        value.addEventListener("highlight", this.highlight.bind(this));
        value.addEventListener("unhighlight", this.unhighlight.bind(this));
        this.classList.remove("highlighted");
        this.render();
    }

    /**
     * 
     * @param model {VNetModel}
     */
    unregister(model) {
        if (model) {
            model.removeEventListener("add", this.render.bind(this));
            model.removeEventListener("remove", this.render.bind(this));
            model.removeEventListener("highglight", this.highlight.bind(this));
            model.removeEventListener("unhighglight", this.unhighlight.bind(this));
        }    
    }
    
    connectedCallback() {
        const overflow = "visible";
        
        this.style.display = "flex";
        this.style.alignItems = "stretch";
        this.style.alignContent = "stretch";
        this.style.flexWrap = "nowrap";
        this.style.flexDirection = "column";
        this.style.boxSizing = "border-box";
        this.style.overflow = overflow;

        const titleBar = document.createElement("div");
        titleBar.style.display = "flex";
        titleBar.style.flexDirection = "row";
        titleBar.style.justifyContent = "space-between";
        titleBar.style.overflow = overflow;

        this.appendChild(titleBar);

        const title = document.createElement("div");
        title.innerHTML = this.vnet?.name ?? "";
        titleBar.appendChild(title);
        this._title = title;
        
        const range = document.createElement("span");
        range.innerHTML = this.range?.info();
        titleBar.appendChild(range);
        
        titleBar.addEventListener("dblclick", this.startEditName.bind(this));

        const button = document.createElement("button");
        button.style.cursor = "pointer";
        button.innerHTML = "/";
        button.addEventListener("click", this.clicked.bind(this));
        titleBar.append(button);

        const direction = this.direction;
        switch(direction) {
            case "horizontal":
                 this.style.height = "100%";
                break;
            case "vertical":
                this.style.width = "100%";
                break;
        }
        // this.style.flexGrow = "1";
        
        const container = document.createElement("div");
        container.style.display = "flex";
        container.style.alignItems = "stretch";
        container.style.alignContent = "stretch";
        container.style.justifyContent = "center";
        container.style.flexWrap = "nowrap";
        container.style.flexDirection = direction === "horizontal" ? "row" : "column";
        container.style.height = "100%";
        container.style.boxSizing = "border-box";
        container.style.overflow = overflow;
        this.appendChild(container);
        this._container = container;
        this._initiated = true;
        this.render();
    }
    
    _initiated = false;
    
    render() {
        if (!this._initiated) {
            return ;
        }
        
        if (!this.model) {
            this._title.innerHTML = "";
            return;
        }

        this.removeChildren();
        if (!this.parent) {
            const range = this.model.getParentRange();
            if (!this.range.equals(range)) {
                this.removeChildren();
                this.range = range;
            }
            const dimension = this.dimension;
            this.style.position = "absolute";
            this.style.display = "block";
            this.style.minHeight = `${dimension.height}px`;
            this.style.minWidth = `${dimension.width}px`;
            this.style.top = `${dimension.height / 2}px`;
            this.style.left = `${dimension.width / 2}px`;
        }
        

        const entries = this.model.getEntries(this.range);
        this._entries = entries;
        switch(entries.length) {
            case 0:
                this._name = undefined;
                this.classList.remove("registered", "conflict");
                break;
            case 1:
                this.classList.add("registered");
                this._name = entries[0].name;
                break;
            default:
                this.classList.add("registered", "conflict");
                this._name = entries.map(e => e.name).join(", ");
                break;
        }
        
        this._title.innerHTML = this._name || "";
        
        if (this._name) {
            this.classList.add("registered");
        }
        else {
            this.classList.remove("registered");
        }
        
        if (this.model.hasSubnetsOf(this.range)) {
            this.split();
        }
        else {
            this.removeChildren();
        }
    }
    
    startEditName() {
        const input = document.createElement("input");
        input.type = "text";
        input.value = this.name ?? "";
        input.addEventListener("keyup", this.endEditName.bind(this));
        this._title.innerHTML = "";
        this._title.appendChild(input);
    }

    /**
     * 
     * @param e {KeyboardEvent}
     */
    endEditName(e) {
        switch(e.key){
            case "Enter":
            case "Return":
                const value = e.target.value;
                this.name = value;
                e.target.removeEventListener("keyup", this.endEditName.bind(this));
                this.render();
                break;
            case "Escape":
                e.target.removeEventListener("keyup", this.endEditName.bind(this));
                this.render();
                break;
        }
    }

    clicked() {
        if (this._left || this._right) {
            this.removeChildren(true);
            return;
        }
        this.split();
    }

    /**
     * @param removeFromModel {Boolean}
     */
    remove(removeFromModel) {
        if (this.name && removeFromModel === true) {
            this.model.remove(this.name, this.range);
        }
        this.removeChildren(removeFromModel);
        super.remove();
    }
    
    get dimension() {
        const log2 = Math.log2(this.range.size);
        if (log2 % 2 === 0) {
            const side = Math.sqrt(this.range.size);
            return {
                height: side,
                width: side
            };
        }
        const short = Math.sqrt(this.range.size / 2);
        const long = short * 2;
        
        switch(this.direction) {
            case "horizontal":
                return {
                    height: short,
                    width: long
                };
            default:
                return {
                    height: long,
                    width: short
                };
        }
    }

    disconnectedCallback() {
        this.unregister(this.model);
    }

    attributeChangedCallback(name, oldValue, newValue) {
        switch(name) {
            case "cidr":
                const range = IpRange.parse(newValue);
                if (!range){
                    return;
                }
                this.range = range;
                break;
        }
    }
}

customElements.define('v-net', VNetElement);

class VNetTableElement extends HTMLElement {
    
    constructor() {
        super();
        this.addEventListener("paste", this.paste.bind(this));
        this._entryMap = new Map();
    }
    
    async copyToClipboard() {
        if (!this.model.entries){
            return;
        }
        const text = this.model.entries.map(e => e.format()).join("\n");
        await navigator.clipboard.writeText(text);
    }
    
    paste(e) {
        const text = e.clipboardData.getData("text/plain");
        const lines = text.split("\n");
        const entries = [];
        for (let ii=0; ii<lines.length; ii++) {
            const line = lines[ii];
            const parts = line.split(",");
            
            if (parts.length < 2) {
                console.log(`Invalid line: ${line}`);
                continue;
            }
            const cidrs = parts[1].split(";").map(c => c.trim());
            const entry = VNetEntry.parse(parts[0], cidrs);
            
            if (entry) {
                entries.push(entry);    
            }
        }
        
        if (entries.length === 0) {
            console.log("Nothing to paste");
            return;
        }
        this.model.entries = entries;
    }

    /**
     * @returns {VNetModel}
     */
    get model() {
        return this._model;
    }
    
    set model(value) {
        
        this._model = value;
        value.addEventListener("add", this.render.bind(this));
        value.addEventListener("remove", this.render.bind(this));
        value.addEventListener("highlight", this.highlight.bind(this));
        value.addEventListener("unhighlight", this.unhighlight.bind(this));
        this.render();
    }
    
    highlight(e) {
        const tr = this._entryMap.get(e);
        if (tr){
            tr.classList.add("highlighted");
        }
    }
    
    unhighlight(e) {
        const tr = this._entryMap.get(e);
        if (tr){
            tr.classList.remove("highlighted");
        }
    }

    connectedCallback() {
        this.innerHTML = `
        <table>
        <thead>
        <tr>
            <th>name</th>
            <th>cidr</th>
            <th>size</th>
            <th></th>
        </tr>
        </thead>
        <tbody>
        </tbody>
        </table>
        <button id="copy">Copy to clipboard</button>
        `;
        this._tbody = this.getElementsByTagName("tbody")[0];

        const copy = this.getElementsByTagName("button")[0];
        copy.addEventListener("click", this.copyToClipboard.bind(this));

        const tr = document.createElement("tr");

        tr.innerHTML = `
        <td><input type="text" placeholder="name"></td>
        <td><input type="text" placeholder="10.10.0.0/16"></td>
        <td></td>
        <td><button>+</button></td>`;
        
        const nameInput = tr.getElementsByTagName("input")[0];
        const cidrInput = tr.getElementsByTagName("input")[1];
        const addButton = tr.getElementsByTagName("button")[0];
        addButton.addEventListener("click", () => {
            if (this.model.add(nameInput.value, cidrInput.value)) {
                nameInput.value = null;
                cidrInput.value = null;
            }
        });
        this._tr = tr;
        
        
        
        this._initiated = true;
        this.render();
    }
    
    disconnectedCallback() {
        const model = this.model;
        if (model) {
            model.removeEventListener("add", this.render.bind(this));
            model.removeEventListener("remove", this.render.bind(this));
            model.removeEventListener("highlight", this.highlight.bind(this));
            model.removeEventListener("unhighlight", this.unhighlight.bind(this));    
        }
    }
    
    _initiated = false;
    
    render() {
        
        if (!this._initiated || !this.model) {
            return;
        }
        
        this._tbody.innerHTML = "";
        this._entryMap.clear();
        
        for (let ii=0; ii<this.model.entries.length; ii++) {
            const entry = this.model.entries[ii];
            const tr = document.createElement("tr");
            tr.innerHTML = `<td>${entry.name}</td><td>${entry.ranges.map(r => r.format()).join(", ")}</td><td>${entry.size}</td><td><button>D</button></td>`;
            const deleteButton = tr.getElementsByTagName("button")[0];
            deleteButton.addEventListener("click", () => {
                this.model.removeEntry(entry);
            });
            tr.addEventListener("mouseenter", () => {
                tr.classList.add("highlighted");
                this.model.highlightEntry(entry);
            });
            tr.addEventListener("mouseleave", () => {
                tr.classList.remove("highlighted");
                this.model.unhighlightEntry(entry);
            });
            this._entryMap.set(entry, tr);
            
            this._tbody.appendChild(tr);
        }
        
        this._tbody.appendChild(this._tr);
    }
}

customElements.define('v-net-table', VNetTableElement);

class VNetEntry {

    /**
     * @param name {string}
     * @param ranges {IpRange[]}
     */
    constructor(name, ranges) {
        this.name = name;
        this.ranges = ranges;
        let sum = 0;
        
        for (let ii=0; ii<ranges.length; ii++) {
            sum += ranges[ii].size;
        }
        this._size = sum;
    }

    /**
     * 
     * @returns {number}
     */
    get size(){
        return this._size;
    }
    
    /**
     * @returns {string}
     */
    get name(){
        return this._name;
    }
    set name(value) {
        this._name = value;
    }

    /**
     * @returns {IpRange[]}
     */
    get ranges() {
        return this._range;
    }
    
    set ranges(value) {
        this._range = value;
    }

    /**
     * 
     * @param name {string}
     * @param cidrs {string[]}
     * @returns {VNetEntry|undefined}
     */
    static parse(name, cidrs) {
        if (!name) {
            return undefined;
        }
        if (!cidrs) {
            return undefined;
        }
        
        const ranges = cidrs.map(c => IpRange.parse(c));
        if (ranges.some(r => !r)){
            return undefined;
        }
        
        return new VNetEntry(name, ranges);
    }

    /**
     * @returns {string}
     */
    format() {
        const ipranges = this.ranges.map(r => r.format()).join(";");
        return `${this.name},${ipranges}`;
    }
}

class VNetModel {
    
    constructor(entries) {
        entries = entries || [];
        /**
         * @type {[VNetEntry]}
         * @private
         */
        this._entries = [];
        for(let ii=0; ii<entries.length; ii++) {
            const e = entries[ii];
            const entry = VNetEntry.parse(e.name, e.cidrs);
            if (!entry){
                continue;
            }
            this._entries[ii] = entry;
        }
        
        const listeners = new Map();
        listeners.set("add", []);
        listeners.set("remove", []);
        listeners.set("highlight", []);
        listeners.set("unhighlight", []);
        this._eventListeners = listeners;
    }

    /**
     * @returns {[VNetEntry]}
     */
    get entries() {
        return this._entries;
    }

    /**
     * 
     * @param value {[VNetEntry]}
     */
    set entries(value) {
        this._entries = value;
        this.fire("add", value);
    }

    getParentRange() {
        const ranges = this._entries.flatMap(e => e.ranges);
        if (ranges.length === 0) {
            // ¯\_(ツ)_/¯
            return IpRange.parse("10.0.0.0/16");
        }
        /** 
         * @type {IpRange | undefined}
         */
        
        
        let parent = ranges[0].parent;
        
        
        let happy = p => p && ranges.every(r => p.contains(r));
        
        while(!happy(parent)) {

            if (!parent.parent) {
                debugger;
            }
            parent = parent.parent;
        }
        
        return parent;
    }

    /**
     * @param range {IpRange}
     */
    hasSubnetsOf(range) {
        
        for (let ii=0; ii<this._entries.length; ii++) {
            const element = this._entries[ii];
            
            for (let jj=0; jj<element.ranges.length; jj++){
                const elementRange = element.ranges[jj];
                if (!range.contains){
                    debugger;
                    console.log("OMG NOT CONTAINS");
                }
                if (range.contains(elementRange) && !range.equals(elementRange)) {
                    return true;
                }    
            }
        }
        
        return false;
    }
    
    clear() {
        while(this._entries.length > 0){
            this._entries.pop();
        }
        this.fire("remove", this._entries);
    }
    
    highlightEntry(entry) {
        this.fire("highlight", entry);
    }
    
    unhighlightEntry(entry) {
        this.fire("unhighlight", entry);
    }
    
    /**
     * @param name {string}
     * @param cidr {string}
     */
    add(name, cidr) {
        const cidrs = cidr.split(",").map(c => c.trim());
        const entry = VNetEntry.parse(name, cidrs);
        if (!entry){
            return false;
        }
        
        this._entries.push(entry);
        this.fire("add", entry);
        return true;
    }
    
    remove(name, range) {
        let ii = 0;
        while (ii < this._entries.length) {
            const entry = this._entries[ii];
            
            let jj = 0;
            while(jj< entry.ranges.length) {
                const entryRange = entry.ranges[jj];
                if (range.equals(entryRange)){
                    entry.ranges.splice(jj, 1);
                }
                jj++;
            }
            
            if (entry.ranges.length === 0) {
                this._entries.splice(ii, 1);
                this.fire("remove", entry);
            }
            else {
                ii++;
            }
        }
    }
    
    removeEntry(entry) {
        const index = this._entries.indexOf(entry);
        if (index < 0) {
            return;
        }
        this._entries.splice(index, 1);
        this.fire("remove", entry);
    }

    /**
     * 
     * @param range
     * @returns {VNetEntry[]}
     */
    getEntries(range) {
        return this._entries.filter(e => e.ranges.some(r => r.equals(range)));
    }
    
    fire(eventName, value) {
        const listeners = this._eventListeners.get(eventName);
        if (!listeners){
            return;
        }
        for (let ii=0; ii<listeners.length; ii++) {
            const listener = listeners[ii];
            if (!listener) {
                console.log(`${listener} is not a function`);
                continue;
            }
            listener(value);
        }
    }
    
    addEventListener(eventName, callback) {
        if (!this._eventListeners.has(eventName)){
            return;
        }
        if (!callback){
            console.log("OMG NOT CALLBACK LOL!");
            return;
        }
        this._eventListeners.get(eventName).push(callback);
    }
    
    removeEventListener(eventName, callback) {
        if (!this._eventListeners.has(eventName)){
            return;
        }
        const listeners = this._eventListeners.get(eventName);
        const index = listeners.indexOf(callback);
        if (index < 0) {
            return;
        }
        listeners.splice(index, 1);
    }
    
    save() {
        const elements = this._entries.map(e => {
            return {
                name: e.name,
                cidrs: e.ranges.map(r => r.format())    
            }
        });
        window.localStorage.setItem("vnet-table", JSON.stringify(elements));
    }
    
    static load() {
        const json = window.localStorage.getItem("vnet-table") || "[]";
        return new VNetModel(JSON.parse(json));
    }

    static load(entries) {
        return new VNetModel(entries);
    }
}