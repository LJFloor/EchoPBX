export interface CrmIntegration {
    id: string;
    name: string;
    logCalls: boolean;
    searchCallerId: boolean;
    provider: CrmProvider;
    configuration: any;
}

export enum CrmProvider {
    EspoCRM = 1,
    HubSpot = 2,
    Krayin = 3,
    Salesforce = 4,
    Teamleader = 5,
    Zoho = 6,
    Pipedrive = 7,
    SynologyContacts = 8,
}

export const CrmProviders = [
    { label: 'TeamLeader', value: CrmProvider.Teamleader, image: '/crm/teamleader.svg', supportsCallLogging: true, supportsCallerIdSearch: true, },
    { label: 'HubSpot', value: CrmProvider.HubSpot, image: '/crm/hubspot.webp', supportsCallLogging: false, supportsCallerIdSearch: true, },
    // { label: 'EspoCRM', value: CrmProvider.EspoCRM, image: '/crm/espocrm.svg', supportsCallLogging: true, supportsCallerIdSearch: true, },
    // { label: 'Krayin', value: CrmProvider.Krayin, image: '/crm/krayin.png', supportsCallLogging: false, supportsCallerIdSearch: true, },
    // { label: 'Salesforce', value: CrmProvider.Salesforce, image: '/crm/salesforce.png' },
    // { label: 'Zoho', value: CrmProvider.Zoho, image: '/crm/zoho.ico' },
    // { label: 'Pipedrive', value: CrmProvider.Pipedrive, image: '/crm/pipedrive.webp' },
    // { label: 'Synology Contacts', value: CrmProvider.SynologyContacts, image: '/crm/synology-contacts.png' },
];